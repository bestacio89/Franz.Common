#nullable enable
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Reflection;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Hosting
{
  public class KafkaListener : IListener
  {
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ILogger<KafkaListener> _logger;
    private readonly string _topicName;

    public KafkaListener(
        IConsumer<Ignore, string> consumer,
        IAssemblyAccessor assemblyAccessor,
        ILogger<KafkaListener> logger)
    {
      _consumer = consumer;
      _logger = logger;
      var assembly = assemblyAccessor.GetEntryAssembly();
      _topicName = TopicNamer.GetTopicName((System.Reflection.Assembly)assembly);
    }

    public event EventHandler<MessageEventArgs>? Received;

    public void Listen()
    {
      _consumer.Subscribe(_topicName);

      while (true)
      {
        try
        {
          var consumeResult = _consumer.Consume();

          var message = new Message
          {
            Headers = TransfertHeaders(consumeResult),
            Body = consumeResult.Message.Value ?? string.Empty, // ✅ Ensure non-null
          };

          _logger.LogInformation(
              "Consumed message from topic {Topic}, partition {Partition}, offset {Offset}, key {Key}",
              consumeResult.Topic,
              consumeResult.Partition,
              consumeResult.Offset,
              "<ignore>" // ✅ since key is Confluent.Kafka.Ignore
          );

          Received?.Invoke(this, new MessageEventArgs(message));
        }
        catch (ConsumeException e)
        {
          _logger.LogError(e,
              "Kafka consume error on topic {Topic}, partition {Partition}: {Reason}",
              e.ConsumerRecord?.Topic ?? _topicName,
              e.ConsumerRecord?.Partition.Value ?? -1,
              e.Error.Reason);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Unexpected error occurred in Kafka listener for topic {Topic}", _topicName);
        }
      }
    }




    public void StopListen()
    {
      _consumer.Unsubscribe();
    }

    public static MessageHeaders TransfertHeaders(ConsumeResult<Ignore, string> consumeResult)
    {
      var dictionary = consumeResult.Message.Headers
          .ToDictionary(
              header => header.Key ?? string.Empty, // Ensure key is not null
              header => new StringValues(
                  Encoding.UTF8.GetString(header.GetValueBytes() ?? Array.Empty<byte>())
              )
          );

      return new MessageHeaders(dictionary);
    }
  }
}
