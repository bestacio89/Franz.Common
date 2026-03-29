using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public interface IConnectionProvider : IAsyncDisposable
{
    ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
}
