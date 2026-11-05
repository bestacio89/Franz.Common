using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.MassTransit.Contracts;

public interface IKafkaConsumer
{
    Task SubscribeAsync<T>(
        string topic,
        Func<IServiceProvider, T, Task> handler,
        CancellationToken cancellationToken = default);
}
