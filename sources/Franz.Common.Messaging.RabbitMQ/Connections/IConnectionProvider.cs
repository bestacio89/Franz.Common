using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public interface IConnectionProvider
{
    IConnection Current { get; }
}
