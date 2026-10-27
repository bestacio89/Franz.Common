#nullable enable
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ.Replay;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;

/// <summary>
/// Fully async, thread-safe RabbitMQ Listener.
/// Senior Note: Integrated with ChannelPool and supports readiness signaling for tests.
/// </summary>
public sealed class RabbitMQListener(
    IChannelPool channelPool,
    IAssemblyAccessor assemblyAccessor,
    ILogger<RabbitMQListener> logger,
    IReplayStrategy? replayStrategy = null) : IListener, IAsyncDisposable
{
  private readonly string _queueName = QueueNamer.GetQueueName(assemblyAccessor.GetEntryAssembly());
  private readonly object _lock = new();
  private CancellationTokenSource? _cts;
  private Task? _listenTask;
  private IChannel? _channel;

  // Signals that the listener is fully consuming
  private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  /// <summary> Wait until the listener is ready and consuming messages </summary>
  public Task WaitUntilReadyAsync() => _readyTcs.Task;

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    lock (_lock)
    {
      if (_listenTask != null) return; // already running
      _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
      _listenTask = Task.Run(() => RunAsync(_cts.Token));
    }

    await Task.CompletedTask; // return immediately; listener runs in background
  }

  private async Task RunAsync(CancellationToken ct)
  {
    try
    {
      _channel = await channelPool.GetAsync(ct).ConfigureAwait(false);
      var consumer = new RabbitMqConsumer(_channel, _queueName);

      // Signal readiness so tests can safely publish
      _readyTcs.TrySetResult();

      await foreach (var e in consumer.ConsumeAsync(ct).ConfigureAwait(false))
      {
        await ReceiveMessageAsync(_channel, e, ct).ConfigureAwait(false);
      }
    }
    catch (OperationCanceledException)
    {
      logger.LogInformation("🛑 RabbitMQ Listener cancellation requested for {Queue}", _queueName);
    }
    catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ct.IsCancellationRequested)
    {
      // RabbitMQ may report consumer/channel shutdown as an interrupted operation
      // while cancellation is closing the consumer. Treat only this cancellation
      // path as an expected stop; interruptions during normal operation still fail.
      logger.LogDebug(ex, "RabbitMQ consumer stopped during cancellation for {Queue}", _queueName);
    }
    catch (Exception ex)
    {
      logger.LogCritical(ex, "🚨 Fatal error in RabbitMQ Listener stream for {Queue}", _queueName);
      throw;
    }
    finally
    {
      if (_channel != null)
      {
        channelPool.Return(_channel);
        _channel = null;
      }

      lock (_lock)
      {
        _listenTask = null;
        _cts?.Dispose();
        _cts = null;
      }
    }
  }

  private async Task ReceiveMessageAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
  {
    try
    {
      var message = new Message
      {
        Headers = RabbitHeaderMapper.ExtractHeaders(delivery),
        Body = Encoding.UTF8.GetString(delivery.Body.Span)
      };

      var handler = OnMessageReceivedAsync
          ?? throw new InvalidOperationException(
              $"No message handler is configured for RabbitMQ queue '{_queueName}'.");

      await handler(new MessageEventArgs(message)).ConfigureAwait(false);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
      // Leave the delivery unacknowledged. Closing the channel lets RabbitMQ redeliver it.
      throw;
    }
    catch (Exception processingException)
    {
      logger.LogError(processingException,
          "Message processing failed for delivery {DeliveryTag} on queue {Queue}",
          delivery.DeliveryTag, _queueName);

      if (replayStrategy is null)
      {
        await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false,
            cancellationToken: ct).ConfigureAwait(false);
        return;
      }

      try
      {
        // Replay strategies publish the failed delivery elsewhere; acknowledgement
        // remains the listener's responsibility so there is exactly one owner.
        await replayStrategy.ReplayAsync(delivery, processingException).ConfigureAwait(false);
      }
      catch (Exception replayException)
      {
        logger.LogError(replayException,
            "Replay strategy failed for delivery {DeliveryTag} on queue {Queue}",
            delivery.DeliveryTag, _queueName);

        await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false,
            cancellationToken: ct).ConfigureAwait(false);
        return;
      }
    }

    // Keep acknowledgement outside the handler/replay catch. A broker/channel
    // acknowledgement failure must not be misclassified as a business failure.
    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: ct)
        .ConfigureAwait(false);
  }

  public async Task StopListenAsync(CancellationToken cancellationToken = default)
  {
    Task? runningTask;
    lock (_lock)
    {
      if (_cts == null) return; // not running
      _cts.Cancel();
      runningTask = _listenTask;
    }

    if (runningTask != null)
    {
      await runningTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    lock (_lock)
    {
      _listenTask = null;
      _cts?.Dispose();
      _cts = null;
    }

    logger.LogInformation("✅ RabbitMQ Listener stopped for {Queue}.", _queueName);
  }

  public async ValueTask DisposeAsync()
  {
    await StopListenAsync().ConfigureAwait(false);
    GC.SuppressFinalize(this);
  }
}