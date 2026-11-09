using System.Threading.Channels;
using Grpc.Core;

namespace Franz.Common.Grpc.AspNetCore.Interceptors;

/// <summary>
/// Bridges the push-based gRPC response writer to the pull-based Franz streaming
/// pipeline. A bounded channel of one message preserves backpressure and snapshots
/// per-message write options so producer updates cannot affect an earlier message.
/// </summary>
internal sealed class GrpcServerStreamBridge<TMessage>(CancellationToken cancellationToken)
    : IServerStreamWriter<TMessage>
{
    private readonly Channel<(TMessage Message, WriteOptions? WriteOptions)> _channel =
        Channel.CreateBounded<(TMessage Message, WriteOptions? WriteOptions)>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            });

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(TMessage message)
        => _channel.Writer.WriteAsync((message, WriteOptions), cancellationToken).AsTask();

    public IAsyncEnumerable<(TMessage Message, WriteOptions? WriteOptions)> ReadAllAsync(
        CancellationToken readerCancellationToken)
        => _channel.Reader.ReadAllAsync(readerCancellationToken);

    public void Complete(Exception? exception = null)
        => _channel.Writer.TryComplete(exception);
}
