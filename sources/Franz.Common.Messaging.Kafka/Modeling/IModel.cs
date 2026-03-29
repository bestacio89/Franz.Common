using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.KafKa.Modeling;

public interface IModel: IAsyncDisposable
{
    ValueTask Produce<TMessage>(string topic, TMessage message, CancellationToken cancel);
}
