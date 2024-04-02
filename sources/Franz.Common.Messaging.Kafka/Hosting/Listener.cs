using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Reflection;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka.Hosting
{
  public class KafkaListener : IListener
  {
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ILogger<KafkaListener> _logger;
    private readonly string _topicName;
    private readonly IMessageDeserializer _deserializer;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public KafkaListener(
      IConsumer<Ignore, string> consumer,
      IAssemblyAccessor assemblyAccessor,
      ILogger<KafkaListener> logger,
      IMessageDeserializer deserializer) // Added deserializer injection
    {
      _consumer = consumer;
      _logger = logger;
      var assembly = assemblyAccessor.GetEntryAssembly();
      _topicName = TopicNamer.GetTopicName(assembly);
      _deserializer = deserializer; // Assign injected deserializer
    }

    public event EventHandler<MessageEventArgs>? Received;

    public async Task ListenAsync() // Use async/await for cleaner loop management
    {
      _consumer.Subscribe(_topicName);

      try
      {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
          try
          {
            var consumeResult = await _consumer.TryConsumeAsync(_cancellationTokenSource.Token, TimeSpan.FromMilliseconds(YOUR_TIMEOUT_VALUE)); // Set your desired timeout

            if (consumeResult) // Check if message was consumed
            {
              var message = new Message
              {
                Headers = TransfertHeaders(consumeResult),
                Body = _deserializer.Deserialize(consumeResult.Value) // Use deserializer
              };
              Received?.Invoke(this, new MessageEventArgs(message)); // Pass deserialized message
            }
            else
            {
              // Handle timeout scenario (e.g., log or throw exception)
              _logger.LogDebug("Consume operation timed out."); // Log timeout for debugging
            }
          }
          catch (ConsumeException e)
          {
            _logger.LogError(e.Error.ToString(), null);
          }
        }
      }
      catch (OperationCanceledException)
      {
        // Handle graceful termination (optional)
      }
      finally
      {
        _consumer.Unsubscribe();
      }
    }

    public void StopListen()
    {
      _cancellationTokenSource.Cancel(); // Signal termination
    }

    public static MessageHeaders TransfertHeaders(ConsumeResult<Ignore, string> consumeResult)
    {
      var dictionary = consumeResult.Headers
        .ToDictionary(x => x.Key, x => new StringValues(Encoding.UTF8.GetString(x.GetValueBytes())));

      var result = new MessageHeaders(dictionary);

      return result;
    }
  }
}
