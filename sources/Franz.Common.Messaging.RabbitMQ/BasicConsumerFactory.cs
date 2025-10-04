using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ;

public class BasicConsumerFactory : IBasicConsumerFactory
{
    public EventingBasicConsumer Build(IModel model)
    {
        EventingBasicConsumer? result = new(model);

        return result;
    }
}
