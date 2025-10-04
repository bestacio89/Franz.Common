using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public interface IModelProvider
{
    IModel Current { get; }
}
