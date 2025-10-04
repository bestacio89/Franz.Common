using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public interface IConnectionFactoryProvider
{
    IConnectionFactory Current { get; }
}
