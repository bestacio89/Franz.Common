#nullable enable
using System.Text;
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
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
  private readonly IProducer<string, byte[]>? _deadLetterProducer;
  private readonly KafkaFailureOptions _failureOptions;

  public KafkaMessageListener(
      IConsumer<string, string> consumer,
      IEnumerable<string> topics,
      IMessageSerializer serializer,
      ILogger<KafkaMessageListener> logger)
  {
    _consumer = consumer;
    _topics = topics.ToArray();
    _serializer = serializer;
    _logger = logger;
    _failureOptions = new KafkaFailureOptions();
  }

  public KafkaMessageListener(
      IConsumer<string, string> consumer,
      IEnumerable<string> topics,
      IMessageSerializer serializer,
      ILogger<KafkaMessageListener> logger,
      IProducer<string, byte[]> deadLetterProducer,
      IOptions<KafkaMessagingOptions> options)
      : this(consumer, topics, serializer, logger)
  {
    _deadLetterProducer = deadLetterProducer;
    _failureOptions = options.Value.Failure ?? new KafkaFailureOptions();
  }

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    if (OnMessageReceivedAsync is null)
      throw new InvalidOperationException("A message handler must be configured before the Kafka listener starts.");

    if (_topics.Length == 0 || _topics.Any(string.IsNullOrWhiteSpace))
      throw new InvalidOperationException("At least one non-empty Kafka topic must be configured.");

    if (_failureOptions.MaxRetryAttempts < 0 || _failureOptions.RetryBackoffMs < 0)
      throw new InvalidOperationException("Kafka failure retry settings cannot be negative.");

    _consumer.Subscribe(_topics);
    _logger.LogInformation("Kafka listener subscribed to {Topics}", string.Join(", ", _topics));

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        ConsumeResult<string, string>? result;
        try
        {
          result = _consumer.Consume(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
          break;
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Kafka consume failed; the listener will continue polling.");
          continue;
        }

        if (result is null)
          continue;

        // A null Kafka value is a valid tombstone for compacted topics.
        if (result.Message?.Value is null)
        {
          _logger.LogDebug("Ignoring Kafka tombstone on {Topic}[{Partition}] offset {Offset}.",
              result.Topic, result.Partition.Value, result.Offset.Value);
          _consumer.Commit(result);
          continue;
        }

        Message? transport;
        try
        {
          transport = _serializer.Deserialize<Message>(result.Message.Value);
          if (transport is null)
            throw new InvalidDataException("The serialized Kafka value deserialized to null.");
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
        for (var attempt = 0; attempt <= _failureOptions.MaxRetryAttempts; attempt++)
        {
          stoppingToken.ThrowIfCancellationRequested();
          try
          {
            await OnMessageReceivedAsync(new MessageEventArgs(transport));
            succeeded = true;
            break;
          }
          catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
          {
            throw;
          }
          catch (Exception ex)
          {
            processingFailure = ex;
            _logger.LogWarning(ex,
                "Kafka handler failed for message {MessageId} at {Topic}[{Partition}] offset {Offset}; attempt {Attempt} of {TotalAttempts}.",
                transport.Id, result.Topic, result.Partition.Value, result.Offset.Value,
                attempt + 1, _failureOptions.MaxRetryAttempts + 1);

            if (attempt < _failureOptions.MaxRetryAttempts && _failureOptions.RetryBackoffMs > 0)
              await Task.Delay(_failureOptions.RetryBackoffMs, stoppingToken);
          }
        }

        if (!succeeded)
        {
          await DeadLetterAndCommitAsync(
              result,
              processingFailure ?? new InvalidOperationException("Kafka message processing failed."),
              stoppingToken);
          continue;
        }

        // Commit only after the complete processing pipeline succeeds.
        _consumer.Commit(result);
      }
    }
    finally
    {
      _consumer.Close();
      _logger.LogInformation("Kafka listener stopped.");
    }
  }

  private async Task DeadLetterAndCommitAsync(
      ConsumeResult<string, string> result,
      Exception failure,
      CancellationToken cancellationToken)
  {
    if (!_failureOptions.EnableDeadLetter)
      throw new InvalidOperationException(
          $"Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} failed and dead-lettering is disabled. Its offset was not committed.",
          failure);

    if (_deadLetterProducer is null)
      throw new InvalidOperationException(
          $"Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} failed, but no dead-letter producer is registered. Its offset was not committed.",
          failure);

    var deadLetterTopic = string.IsNullOrWhiteSpace(_failureOptions.DeadLetterTopic)
        ? $"{result.Topic}-dlt"
        : _failureOptions.DeadLetterTopic;

    var headers = new Headers();
    if (result.Message.Headers is not null)
    {
      foreach (var header in result.Message.Headers)
        headers.Add(header.Key, header.GetValueBytes());
    }

    headers.Add("x-franz-original-topic", Encoding.UTF8.GetBytes(result.Topic));
    headers.Add("x-franz-original-partition", Encoding.UTF8.GetBytes(result.Partition.Value.ToString()));
    headers.Add("x-franz-original-offset", Encoding.UTF8.GetBytes(result.Offset.Value.ToString()));
    headers.Add("x-franz-failure-type", Encoding.UTF8.GetBytes(failure.GetType().FullName ?? failure.GetType().Name));
    headers.Add("x-franz-failure-message", Encoding.UTF8.GetBytes(failure.Message));
    headers.Add("x-franz-failure-utc", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")));

    var key = _failureOptions.UseOriginalMessageKey
        ? result.Message.Key
        : Guid.CreateVersion7().ToString();

    try
    {
      await _deadLetterProducer.ProduceAsync(
          deadLetterTopic,
          new Confluent.Kafka.Message<string, byte[]>
          {
            Key = key,
            Value = Encoding.UTF8.GetBytes(result.Message.Value),
            Headers = headers,
            Timestamp = result.Message.Timestamp
          },
          cancellationToken);

      _logger.LogError(failure,
          "Kafka message at {Topic}[{Partition}] offset {Offset} was published to dead-letter topic {DeadLetterTopic}.",
          result.Topic, result.Partition.Value, result.Offset.Value, deadLetterTopic);

      // Acknowledge the original only after the broker confirms DLT publication.
      _consumer.Commit(result);
    }
    catch (Exception deadLetterFailure)
    {
      throw new InvalidOperationException(
          $"Failed to publish Kafka message at {result.Topic}[{result.Partition.Value}] offset {result.Offset.Value} to dead-letter topic '{deadLetterTopic}'. The original offset was not committed.",
          new AggregateException(failure, deadLetterFailure));
    }
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
