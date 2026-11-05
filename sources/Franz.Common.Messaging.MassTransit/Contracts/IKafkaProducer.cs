using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.MassTransit.Contracts;

public interface IKafkaProducer
{
    Task PublishAsync<T>(T message, string topic, CancellationToken cancellationToken = default);
}
