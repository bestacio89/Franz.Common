using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class BasicConsumerFactory : IBasicConsumerFactory
{
  public AsyncEventingBasicConsumer BuildAsync(IChannel channel)
  {
    return new AsyncEventingBasicConsumer(channel);
  }
}
