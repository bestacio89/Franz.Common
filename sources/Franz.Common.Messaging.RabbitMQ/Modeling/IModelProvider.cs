using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public interface IModelProvider : IAsyncDisposable
{
    ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
}

