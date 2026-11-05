#nullable enable
using System.Text;
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka;
using System.Collections.Concurrent;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

public sealed class KafkaMessageListener : IListener
{
  private readonly IConsumer<string, string> _consumer;
  private readonly ILogger<KafkaMessageListener> _logger;
  private readonly IMessageSerializer _serializer;
  private readonly string[] _topics;
  private readonly IProducer<string, byte[]> _deadLetterProducer;
  private readonly MessageFailurePolicy _failurePolicy;
  private readonly string? _deadLetterTopic;
  private readonly KafkaRebalanceCoordinator _rebalanceCoordinator;
  private readonly string _groupId;
  private readonly ConcurrentQueue<PendingKafkaResult> _pendingResults = new();

  public KafkaMessageListener(
      IConsumer<string, string> consumer,
      IEnumerable<string> topics,
      IMessageSerializer serializer,
      ILogger<KafkaMessageListener> logger,
      IProducer<string, byte[]> deadLetterProducer,
      IOptions<KafkaMessagingOptions> options,
      KafkaRebalanceCoordinator rebalanceCoordinator)
  {
    _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    _topics = topics?.ToArray() ?? throw new ArgumentNullException(nameof(topics));
    _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    ArgumentNullException.ThrowIfNull(options);
    _deadLetterProducer = deadLetterProducer ?? throw new ArgumentNullException(nameof(deadLetterProducer));
    _rebalanceCoordinator = rebalanceCoordinator ?? throw new ArgumentNullException(nameof(rebalanceCoordinator));
    _failurePolicy = options.Value.FailurePolicy;
    _deadLetterTopic = options.Value.DeadLetterTopic;
    _groupId = options.Value.GroupId ?? throw new InvalidOperationException("Kafka GroupId is required for rebalance coordination.");
  }

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    if (OnMessageReceivedAsync is null)
      throw new InvalidOperationException("A message handler must be configured before the Kafka listener starts.");
    if (_topics.Length == 0 || _topics.Any(string.IsNullOrWhiteSpace))
      throw new InvalidOperationException("At least one non-empty Kafka topic must be configured.");

    _consumer.Subscribe(_topics);
    _logger.LogInformation("Kafka listener subscribed to {Topics}", string.Join(", ", _topics));
    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string>? result;
        try
        {
          if (_pendingResults.TryDequeue(out var pending))
          {
            if (pending.RebalanceToken.IsCancellationRequested)
              continue;

            result = pending.Result;
          }
          else
          {
            result = _consumer.Consume(stoppingToken);
          }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Kafka consume failed; the listener will continue polling.");
          continue;
        }

        if (result is null) continue;
        if (result.Message?.Value is null)
        {
          _logger.LogDebug("Ignoring Kafka tombstone on {Topic}[{Partition}] offset {Offset}.",
              result.Topic, result.Partition.Value, result.Offset.Value);
          CommitAfterProcessing(result);
          continue;
        }

        Message? transport;
        try
        {
          transport = _serializer.Deserialize<Message>(result.Message.Value);
          if (transport is null) throw new InvalidDataException("The serialized Kafka value deserialized to null.");
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Kafka message at {Topic}[{Partition}] offset {Offset} could not be deserialized.",
              result.Topic, result.Partition.Value, result.Offset.Value);
          await DeadLetterAndCommitAsync(result, ex, stoppingToken);
          continue;
        }

        Exception? processingFailure = null;
        var succeeded = false;
        var rebalanceInterrupted = false;
        var groupId = _groupId;
        var rebalanceToken = _rebalanceCoordinator.GetRebalanceToken(groupId, result.TopicPartition);

        _rebalanceCoordinator.BeginHandlerExecution(groupId);
        try
        {
          PauseAssignedPartitions();

          for (var attempt = 0; attempt <= _failurePolicy.MaxRetryAttempts; attempt++)
          {
            stoppingToken.ThrowIfCancellationRequested();
            if (rebalanceToken.IsCancellationRequested)
            {
              rebalanceInterrupted = true;
              break;
            }

            try
            {
              using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, rebalanceToken);
              using var handlerTimeout = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
              if (_failurePolicy.HandlerTimeout is { } timeout)
                handlerTimeout.CancelAfter(timeout);

              try
              {
                await InvokeHandlerWhilePollingAsync(transport, handlerTimeout.Token);
              }
              catch (OperationCanceledException ex)
                  when (handlerTimeout.IsCancellationRequested &&
                        !stoppingToken.IsCancellationRequested &&
                        !rebalanceToken.IsCancellationRequested)
              {
                throw new TimeoutException($"Kafka handler exceeded cooperative timeout {_failurePolicy.HandlerTimeout}.", ex);
              }

              succeeded = true;
              break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
              throw;
            }
            catch (OperationCanceledException) when (rebalanceToken.IsCancellationRequested)
            {
              rebalanceInterrupted = true;
              break;
            }
            catch (Exception ex)
            {
              processingFailure = ex;
              _logger.LogWarning(ex,
                  "Kafka handler failed for message {MessageId} at {Topic}[{Partition}] offset {Offset}; attempt {Attempt} of {TotalAttempts}.",
                  transport.Id, result.Topic, result.Partition.Value, result.Offset.Value,
                  attempt + 1, _failurePolicy.MaxRetryAttempts + 1);

              if (attempt < _failurePolicy.MaxRetryAttempts && _failurePolicy.RetryBackoff > TimeSpan.Zero)
              {
                try
                {
                  await DelayWhilePollingAsync(_failurePolicy.RetryBackoff, rebalanceToken, stoppingToken);
                }
                catch (OperationCanceledException) when (rebalanceToken.IsCancellationRequested)
                {
                  rebalanceInterrupted = true;
                  break;
                }
              }
            }
          }
        }
        finally
        {
          _rebalanceCoordinator.EndHandlerExecution(groupId);
          ResumeAssignedPartitions();
        }

        if (rebalanceToken.IsCancellationRequested)
          rebalanceInterrupted = true;

        if (rebalanceInterrupted)
        {
          _logger.LogInformation(
              "Skipping retry and offset commit for message {MessageId} because its Kafka partition was revoked or lost. The record may be redelivered.",
              transport.Id);
          continue;
        }

        if (!succeeded)
        {
          await DeadLetterAndCommitAsync(result,
              processingFailure ?? new InvalidOperationException("Kafka message processing failed."), stoppingToken);
          continue;
        }
        CommitAfterProcessing(result);
      }
    }
    finally
    {
      _consumer.Close();
      _logger.LogInformation("Kafka listener stopped.");
    }
  }

  private async Task InvokeHandlerWhilePollingAsync(Message message, CancellationToken handlerToken)
  {
    // Run handler invocation off the consumer thread so synchronous handler work cannot
    // starve Consume() and trigger MaxPollIntervalMs while the handler is in flight.
    var handlerTask = Task.Run(
        () => OnMessageReceivedAsync!(new MessageEventArgs(message, handlerToken)),
        CancellationToken.None);
    while (!handlerTask.IsCompleted)
    {
      try
      {
        var polled = _consumer.Consume(TimeSpan.FromMilliseconds(100));
        if (polled is not null)
          _pendingResults.Enqueue(new PendingKafkaResult(polled, _rebalanceCoordinator.GetRebalanceToken(_groupId, polled.TopicPartition)));
      }
      catch (ConsumeException ex)
      {
        _logger.LogWarning(ex, "Kafka poll failed while a handler was in flight.");
      }
    }
    await handlerTask.ConfigureAwait(false);
  }

  private Task DelayWhilePollingAsync(
      TimeSpan delay,
      CancellationToken rebalanceToken,
      CancellationToken stoppingToken)
  {
    var remaining = delay;
    while (remaining > TimeSpan.Zero)
    {
      stoppingToken.ThrowIfCancellationRequested();
      rebalanceToken.ThrowIfCancellationRequested();
      var interval = remaining > TimeSpan.FromMilliseconds(100)
          ? TimeSpan.FromMilliseconds(100)
          : remaining;
      try
      {
        var polled = _consumer.Consume(interval);
        if (polled is not null)
          _pendingResults.Enqueue(new PendingKafkaResult(polled, _rebalanceCoordinator.GetRebalanceToken(_groupId, polled.TopicPartition)));
      }
      catch (ConsumeException ex)
      {
        _logger.LogWarning(ex, "Kafka poll failed during retry backoff.");
      }
      remaining -= interval;
    }
    return Task.CompletedTask;
  }

  private sealed record PendingKafkaResult(ConsumeResult<string, string> Result, CancellationToken RebalanceToken);

  private void PauseAssignedPartitions()
  {
    var assignment = _consumer.Assignment;
    if (assignment.Count == 0)
      return;
    try { _consumer.Pause(assignment); }
    catch (KafkaException ex)
    {
      _logger.LogWarning(ex, "Could not pause Kafka partitions while a handler is in flight.");
    }
  }

  private void ResumeAssignedPartitions()
  {
    var assignment = _consumer.Assignment;
    if (assignment.Count == 0)
      return;
    try { _consumer.Resume(assignment); }
    catch (KafkaException ex)
    {
      _logger.LogWarning(ex, "Could not resume Kafka partitions after handler completion.");
    }
  }

  private void CommitAfterProcessing(ConsumeResult<string, string> result)
  {
    try { _consumer.Commit(result); }
    catch (KafkaException ex) when (ex.Error.Code is ErrorCode.Local_State or ErrorCode.IllegalGeneration
        or ErrorCode.UnknownMemberId or ErrorCode.RebalanceInProgress)
    {
      _logger.LogWarning(ex,
          "Kafka offset commit was rejected during rebalance for {Topic}[{Partition}] offset {Offset}. The message may be redelivered.",
          result.Topic, result.Partition.Value, result.Offset.Value);
    }
  }

  private async Task DeadLetterAndCommitAsync(ConsumeResult<string, string> result, Exception failure,
      CancellationToken cancellationToken)
  {
    if (!_failurePolicy.DeadLetterOnExhaustion)
      throw new InvalidOperationException($"Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} failed and dead-lettering is disabled. Its offset was not committed.", failure);

    var deadLetterTopic = string.IsNullOrWhiteSpace(_deadLetterTopic) ? $"{result.Topic}-dlt" : _deadLetterTopic;
    var headers = new Confluent.Kafka.Headers();
    if (result.Message.Headers is not null)
      foreach (var header in result.Message.Headers) headers.Add(header.Key, header.GetValueBytes());
    headers.Add("x-franz-original-topic", Encoding.UTF8.GetBytes(result.Topic));
    headers.Add("x-franz-original-partition", Encoding.UTF8.GetBytes(result.Partition.Value.ToString()));
    headers.Add("x-franz-original-offset", Encoding.UTF8.GetBytes(result.Offset.Value.ToString()));
    headers.Add("x-franz-failure-type", Encoding.UTF8.GetBytes(failure.GetType().FullName ?? failure.GetType().Name));
    headers.Add("x-franz-failure-message", Encoding.UTF8.GetBytes(failure.Message));
    headers.Add("x-franz-failure-utc", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")));

    try
    {
      await _deadLetterProducer.ProduceAsync(deadLetterTopic,
          new Confluent.Kafka.Message<string, byte[]>
          {
            Key = result.Message.Key,
            Value = Encoding.UTF8.GetBytes(result.Message.Value),
            Headers = headers,
            Timestamp = result.Message.Timestamp
          }, cancellationToken);
    }
    catch (Exception deadLetterFailure)
    {
      throw new InvalidOperationException(
          $"Failed to publish Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} to dead-letter topic '{deadLetterTopic}'. The original offset was not committed.",
          new AggregateException(failure, deadLetterFailure));
    }

    _logger.LogError(failure, "Kafka message at {Topic}[{Partition}] offset {Offset} was published to dead-letter topic {DeadLetterTopic}.",
        result.Topic, result.Partition.Value, result.Offset.Value, deadLetterTopic);
    CommitAfterProcessing(result);
  }

  public Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("Kafka consumer unsubscribing.");
      _consumer.Unsubscribe();
      return Task.CompletedTask;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Kafka consumer unsubscribe failed.");
      return Task.FromException(ex);
    }
  }
}