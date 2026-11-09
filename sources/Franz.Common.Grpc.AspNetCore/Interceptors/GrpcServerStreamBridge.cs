using System.Threading.Channels;
using Grpc.Core;

namespace Franz.Common.Grpc.AspNetCore.Interceptors;

/// <summary>
/// Bridges the push-based gRPC response writer to the pull-based Franz streaming
/// pipeline. A bounded channel of one message preserves backpressure.
/// </summary>
internal sealed class GrpcServerStreamBridge<TMessage>(CancellationToken cancellationToken)
    : IServerStreamWriter<TMessage>
{
    private readonly Channel<TMessage> _channel = Channel.CreateBounded<TMessage>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(TMessage message)
        => _channel.Writer.WriteAsync(message, cancellationToken).AsTask();

    public IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken readerCancellationToken)
        => _channel.Reader.ReadAllAsync(readerCancellationToken);

    public void Complete(Exception? exception = null)
        => _channel.Writer.TryComplete(exception);
}
