using Franz.Common.Messaging.RabbitMQ.Modeling;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ;

public interface IBasicConsumerFactory
{

  /// <summary>
  /// Build an asynchronous consumer. Uses AsyncEventingBasicConsumer.
  /// </summary>
  AsyncEventingBasicConsumer BuildAsync(IChannel model);
}
