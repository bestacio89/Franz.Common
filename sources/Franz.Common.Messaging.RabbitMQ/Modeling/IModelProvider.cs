using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public interface IModelProvider
{
    IChannel Current { get; }
}
