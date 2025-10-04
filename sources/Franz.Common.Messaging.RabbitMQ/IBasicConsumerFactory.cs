using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ;

public interface IBasicConsumerFactory
{
    EventingBasicConsumer Build(IModel model);
}
