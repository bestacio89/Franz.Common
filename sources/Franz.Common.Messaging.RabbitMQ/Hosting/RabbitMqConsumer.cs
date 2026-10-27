using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Franz.Common.Messaging.RabbitMQ.Hosting;

/// <summary>
/// Bridges RabbitMQ push deliveries to a bounded, sequential async stream.
/// </summary>
public sealed class RabbitMqConsumer(
    IChannel channel,
    string queueName,
    ushort prefetchCount = 10) : IAsyncDisposable
{
    private readonly Channel<BasicDeliverEventArgs> _messageChannel =
        Channel.CreateBounded<BasicDeliverEventArgs>(new BoundedChannelOptions(prefetchCount)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    private readonly TaskCompletionSource _readyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? _consumerTag;

    /// <summary>Completes after RabbitMQ has accepted the consumer registration.</summary>
    public Task WaitUntilReadyAsync() => _readyTcs.Task;

    public async IAsyncEnumerable<BasicDeliverEventArgs> ConsumeAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        try
        {
            await channel.BasicQosAsync(0, prefetchCount, global: false, cancellationToken: ct)
                .ConfigureAwait(false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, args) =>
            {
                await _messageChannel.Writer.WriteAsync(args, ct).ConfigureAwait(false);
            };

            _consumerTag = await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct).ConfigureAwait(false);

            _readyTcs.TrySetResult();
        }
        catch (Exception ex)
        {
            _readyTcs.TrySetException(ex);
            _messageChannel.Writer.TryComplete(ex);
            throw;
        }

        var reader = _messageChannel.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var delivery))
                yield return delivery;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumerTag is not null && channel.IsOpen)
        {
            try
            {
                await channel.BasicCancelAsync(_consumerTag).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Shutdown can race broker-side consumer cancellation.
            }
        }

        _messageChannel.Writer.TryComplete();
        GC.SuppressFinalize(this);
    }
}