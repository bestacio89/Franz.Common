#nullable enable
using System.Text;
using System.Diagnostics;
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
  private readonly KafkaRebalanceCoordinator.ConsumerRegistration _registration;
  private readonly ConcurrentQueue<PendingKafkaResult> _pendingResults = new();
  private int _deferredConsumerRelease;
  private readonly CancellationTokenSource _stopCts = new();

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
    _registration = _rebalanceCoordinator.GetOrCreateRegistration(_consumer, _groupId);
  }

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    var linkedStopping = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _stopCts.Token);
    var listenerToken = linkedStopping.Token;

    try
    {
      if (OnMessageReceivedAsync is null)
        throw new InvalidOperationException("A message handler must be configured before the Kafka listener starts.");
      if (_topics.Length == 0 || _topics.Any(string.IsNullOrWhiteSpace))
        throw new InvalidOperationException("At least one non-empty Kafka topic must be configured.");

      _consumer.Subscribe(_topics);
      _logger.LogInformation("Kafka listener subscribed to {Topics}", string.Join(", ", _topics));

      while (!listenerToken.IsCancellationRequested)
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
            result = _consumer.Consume(listenerToken);
          }
        }
        catch (OperationCanceledException) when (listenerToken.IsCancellationRequested) { break; }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Kafka consume failed; the listener will continue polling.");
          continue;
        }

        if (result is null) continue;
        var resultRebalanceToken = _rebalanceCoordinator.GetRebalanceToken(_registration, result.TopicPartition);
        if (resultRebalanceToken.IsCancellationRequested)
          continue;

        if (result.Message?.Value is null)
        {
          _logger.LogDebug("Ignoring Kafka tombstone on {Topic}[{Partition}] offset {Offset}.",
              result.Topic, result.Partition.Value, result.Offset.Value);
          CommitAfterProcessing(result, resultRebalanceToken);
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
          try
          {
            await DeadLetterAndCommitAsync(result, ex, resultRebalanceToken, listenerToken);
          }
          catch (OperationCanceledException) when (
              resultRebalanceToken.IsCancellationRequested && !listenerToken.IsCancellationRequested)
          {
            _logger.LogInformation(
                "Skipping dead-letter offset commit for {Topic}[{Partition}] offset {Offset} because its partition was revoked or lost.",
                result.Topic, result.Partition.Value, result.Offset.Value);
          }
          continue;
        }

        Exception? processingFailure = null;
        var succeeded = false;
        var rebalanceInterrupted = false;
        var rebalanceToken = resultRebalanceToken;

        _rebalanceCoordinator.BeginHandlerExecution(_registration);
        try
        {
          PauseAssignedPartitions();

          for (var attempt = 0; attempt <= _failurePolicy.MaxRetryAttempts; attempt++)
          {
            listenerToken.ThrowIfCancellationRequested();
            if (rebalanceToken.IsCancellationRequested)
            {
              rebalanceInterrupted = true;
              break;
            }

            try
            {
              using var linked = CancellationTokenSource.CreateLinkedTokenSource(listenerToken, rebalanceToken);
              using var handlerTimeout = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
              if (_failurePolicy.HandlerTimeout is { } timeout)
                handlerTimeout.CancelAfter(timeout);

              try
              {
                await InvokeHandlerWhilePollingAsync(transport, handlerTimeout.Token, rebalanceToken, listenerToken);
              }
              catch (OperationCanceledException ex)
                  when (handlerTimeout.IsCancellationRequested &&
                        !listenerToken.IsCancellationRequested &&
                        !rebalanceToken.IsCancellationRequested)
              {
                throw new TimeoutException($"Kafka handler exceeded cooperative timeout {_failurePolicy.HandlerTimeout}.", ex);
              }

              succeeded = true;
              break;
            }
            catch (OperationCanceledException) when (listenerToken.IsCancellationRequested)
            {
              throw;
            }
            catch (OperationCanceledException) when (rebalanceToken.IsCancellationRequested)
            {
              rebalanceInterrupted = true;
              break;
            }
            catch (HandlerDidNotStopException)
            {
              // A timed-out or revoked handler that ignores cancellation may still be
              // executing. Stop this consumer instead of retrying or acknowledging work
              // concurrently with the abandoned handler.
              throw;
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
                  await DelayWhilePollingAsync(_failurePolicy.RetryBackoff, rebalanceToken, listenerToken);
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
          if (Volatile.Read(ref _deferredConsumerRelease) == 0)
            _rebalanceCoordinator.EndHandlerExecution(_registration);
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
          try
          {
            await DeadLetterAndCommitAsync(result,
                processingFailure ?? new InvalidOperationException("Kafka message processing failed."),
                rebalanceToken,
                listenerToken);
          }
          catch (OperationCanceledException) when (
              rebalanceToken.IsCancellationRequested && !listenerToken.IsCancellationRequested)
          {
            _logger.LogInformation(
                "Skipping dead-letter offset commit for {Topic}[{Partition}] offset {Offset} because its partition was revoked or lost.",
                result.Topic, result.Partition.Value, result.Offset.Value);
          }
          continue;
        }
        CommitAfterProcessing(result, rebalanceToken);
      }
    }
    finally
    {
      try
      {
        _consumer.Close();
      }
      finally
      {
        if (Volatile.Read(ref _deferredConsumerRelease) == 0)
          _rebalanceCoordinator.ReleaseConsumer(_consumer);
        _logger.LogInformation("Kafka listener stopped.");
        linkedStopping.Dispose();
        _stopCts.Dispose();
      }
    }
  }

  private async Task InvokeHandlerWhilePollingAsync(
      Message message,
      CancellationToken handlerToken,
      CancellationToken rebalanceToken,
      CancellationToken stoppingToken)
  {
    // Run handler invocation off the consumer thread so synchronous handler work cannot
    // starve Consume() and trigger MaxPollIntervalMs while the handler is in flight.
    var handler = OnMessageReceivedAsync
        ?? throw new InvalidOperationException("A message handler must remain configured while the Kafka listener is running.");
    var handlerTask = Task.Run(
        () => handler(new MessageEventArgs(message, handlerToken)),
        CancellationToken.None);

    while (!handlerTask.IsCompleted)
    {
      if (handlerToken.IsCancellationRequested)
      {
        if (stoppingToken.IsCancellationRequested)
        {
          DeferConsumerReleaseUntilHandlerCompletes(handlerTask);
          throw new OperationCanceledException("Kafka listener is stopping while a handler is still running.", stoppingToken);
        }

        var reason = rebalanceToken.IsCancellationRequested
            ? "the Kafka partition was revoked or lost"
            : $"the configured handler timeout {_failurePolicy.HandlerTimeout} elapsed";

        DeferConsumerReleaseUntilHandlerCompletes(handlerTask);
        throw new HandlerDidNotStopException(
            $"Kafka handler did not stop after cancellation because {reason}. The consumer will stop without committing the message.");
      }

      try
      {
        var polled = _consumer.Consume(TimeSpan.FromMilliseconds(100));
        if (polled is not null)
          _pendingResults.Enqueue(new PendingKafkaResult(polled, _rebalanceCoordinator.GetRebalanceToken(_registration, polled.TopicPartition)));
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
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.Elapsed < delay)
    {
      stoppingToken.ThrowIfCancellationRequested();
      rebalanceToken.ThrowIfCancellationRequested();
      var remaining = delay - stopwatch.Elapsed;
      var interval = remaining > TimeSpan.FromMilliseconds(100)
          ? TimeSpan.FromMilliseconds(100)
          : remaining;
      try
      {
        var polled = _consumer.Consume(interval);
        if (polled is not null)
          _pendingResults.Enqueue(new PendingKafkaResult(polled, _rebalanceCoordinator.GetRebalanceToken(_registration, polled.TopicPartition)));
      }
      catch (ConsumeException ex)
      {
        _logger.LogWarning(ex, "Kafka poll failed during retry backoff.");
      }
    }
    return Task.CompletedTask;
  }

  private void DeferConsumerReleaseUntilHandlerCompletes(Task handlerTask)
  {
    Interlocked.Exchange(ref _deferredConsumerRelease, 1);

    _ = handlerTask.ContinueWith(
        completed =>
        {
          if (completed.IsFaulted)
            _logger.LogError(completed.Exception, "A Kafka handler completed with an exception after its consumer had been stopped.");

          _rebalanceCoordinator.EndHandlerExecution(_registration);
          _rebalanceCoordinator.ReleaseConsumer(_consumer);
        },
        CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);
  }

  private sealed class HandlerDidNotStopException(string message) : InvalidOperationException(message);

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

  private void CommitAfterProcessing(
      ConsumeResult<string, string> result,
      CancellationToken rebalanceToken)
  {
    if (rebalanceToken.IsCancellationRequested)
    {
      _logger.LogDebug(
          "Skipping Kafka offset commit for {Topic}[{Partition}] offset {Offset} because its partition is no longer assigned.",
          result.Topic, result.Partition.Value, result.Offset.Value);
      return;
    }

    try { _consumer.Commit(result); }
    catch (KafkaException ex) when (ex.Error.Code is ErrorCode.Local_State or ErrorCode.IllegalGeneration
        or ErrorCode.UnknownMemberId or ErrorCode.RebalanceInProgress)
    {
      _logger.LogWarning(ex,
          "Kafka offset commit was rejected during rebalance for {Topic}[{Partition}] offset {Offset}. The message may be redelivered.",
          result.Topic, result.Partition.Value, result.Offset.Value);
    }
  }

  private async Task DeadLetterAndCommitAsync(
      ConsumeResult<string, string> result,
      Exception failure,
      CancellationToken rebalanceToken,
      CancellationToken cancellationToken)
  {
    if (rebalanceToken.IsCancellationRequested)
    {
      _logger.LogDebug(
          "Skipping dead-letter publication for {Topic}[{Partition}] offset {Offset} because its partition is no longer assigned.",
          result.Topic, result.Partition.Value, result.Offset.Value);
      return;
    }

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

    _rebalanceCoordinator.BeginHandlerExecution(_registration);
    try
    {
      PauseAssignedPartitions();
      try
      {
        var deliveryTask = _deadLetterProducer.ProduceAsync(deadLetterTopic,
            new Confluent.Kafka.Message<string, byte[]>
            {
              Key = result.Message.Key,
              Value = Encoding.UTF8.GetBytes(result.Message.Value),
              Headers = headers,
              Timestamp = result.Message.Timestamp
            }, cancellationToken);

        await AwaitWhilePollingAsync(deliveryTask, cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception deadLetterFailure)
      {
        throw new InvalidOperationException(
            $"Failed to publish Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} to dead-letter topic '{deadLetterTopic}'. The original offset was not committed.",
            new AggregateException(failure, deadLetterFailure));
      }

      _logger.LogError(failure, "Kafka message at {Topic}[{Partition}] offset {Offset} was published to dead-letter topic {DeadLetterTopic}.",
          result.Topic, result.Partition.Value, result.Offset.Value, deadLetterTopic);
      CommitAfterProcessing(result, rebalanceToken);
    }
    finally
    {
      _rebalanceCoordinator.EndHandlerExecution(_registration);
      ResumeAssignedPartitions();
    }
  }

  private async Task AwaitWhilePollingAsync(
      Task deliveryTask,
      CancellationToken cancellationToken)
  {
    while (!deliveryTask.IsCompleted)
    {
      try
      {
        var polled = _consumer.Consume(TimeSpan.FromMilliseconds(100));
        if (polled is not null)
          _pendingResults.Enqueue(new PendingKafkaResult(
              polled,
              _rebalanceCoordinator.GetRebalanceToken(_registration, polled.TopicPartition)));
      }
      catch (ConsumeException ex)
      {
        _logger.LogWarning(ex, "Kafka poll failed while a dead-letter publication was in flight.");
      }
    }

    await deliveryTask.ConfigureAwait(false);
    cancellationToken.ThrowIfCancellationRequested();
    // CommitAfterProcessing checks the captured partition token before acknowledging
    // the source offset, so a publication completed after revocation is not committed.
  }

  public Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    // Cancel the listener token instead of invoking consumer APIs from a host
    // shutdown thread; Confluent.Kafka consumers are single-thread-affine.
    _logger.LogInformation("Kafka listener shutdown requested.");
    try { _stopCts.Cancel(); }
    catch (ObjectDisposedException) { }
    return Task.CompletedTask;
  }
}