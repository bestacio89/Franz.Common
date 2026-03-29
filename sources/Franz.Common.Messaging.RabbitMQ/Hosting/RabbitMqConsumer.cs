using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Franz.Common.Messaging.RabbitMQ.Hosting;

/// <summary>
/// High-performance bridge between RabbitMQ push events and IAsyncEnumerable.
/// Senior Note: Utilizes System.Threading.Channels for thread-safe handoffs and zero-allocation streaming.
/// </summary>
public sealed class RabbitMqConsumer(
    IChannel channel, 
    string queueName, 
    ushort prefetchCount = 10) : IAsyncDisposable
{
    private readonly Channel<BasicDeliverEventArgs> _messageChannel = Channel.CreateUnbounded<BasicDeliverEventArgs>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    private string? _consumerTag;

    public async IAsyncEnumerable<BasicDeliverEventArgs> ConsumeAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. Configure Quality of Service (Back-pressure)
        // Senior Note: prefetchCount determines how many un-acked messages RabbitMQ sends.
        await channel.BasicQosAsync(0, prefetchCount, false, ct).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        
        // 2. Event Handler -> Channel Writer Bridge
        consumer.ReceivedAsync += (sender, args) =>
        {
            // Non-blocking write to the internal buffer
            _messageChannel.Writer.TryWrite(args);
            return Task.CompletedTask;
        };

        // 3. Start the actual RabbitMQ Consumption
        _consumerTag = await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct).ConfigureAwait(false);

        // 4. Yield messages as they arrive in the channel
        var reader = _messageChannel.Reader;
        
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var args))
            {
                yield return args;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _messageChannel.Writer.TryComplete();

        if (_consumerTag != null && channel.IsOpen)
        {
            try 
            { 
                // Stop RabbitMQ from sending more messages
                await channel.BasicCancelAsync(_consumerTag).ConfigureAwait(false); 
            } 
            catch { /* Consumer may already be closed */ }
        }
        
        GC.SuppressFinalize(this);
    }
}