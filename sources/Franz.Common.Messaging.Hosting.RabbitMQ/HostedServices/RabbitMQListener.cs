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

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;

/// <summary>
/// Owns the lifecycle of a RabbitMQ consumer and dispatches deliveries sequentially.
/// </summary>
public sealed class RabbitMQListener(
    IChannelPool channelPool,
    IAssemblyAccessor assemblyAccessor,
    ILogger<RabbitMQListener> logger,
    IReplayStrategy? replayStrategy = null) : IListener, IAsyncDisposable
{
  private readonly string _queueName = QueueNamer.GetQueueName(
      assemblyAccessor.GetEntryAssembly()
      ?? throw new InvalidOperationException("Entry assembly cannot be null."));
  private readonly object _lock = new();
  private CancellationTokenSource? _cts;
  private Task? _listenTask;
  private IChannel? _channel;
  private TaskCompletionSource _readyTcs = NewReadySource();

  public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  /// <summary>Completes when the consumer has started consuming, or faults if startup fails.</summary>
  public Task WaitUntilReadyAsync()
  {
    lock (_lock)
      return _readyTcs.Task;
  }

  /// <summary>The current or most recent consumer task, for host lifecycle supervision.</summary>
  public Task Completion
  {
    get
    {
      lock (_lock)
        return _listenTask ?? Task.CompletedTask;
    }
  }

  /// <summary>Starts consuming and returns once the broker has accepted the consumer.</summary>
  public async Task Listen(CancellationToken stoppingToken = default)
  {
    Task readyTask;
    lock (_lock)
    {
      if (_listenTask is { IsCompleted: false })
      {
        readyTask = _readyTcs.Task;
      }
      else
      {
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _readyTcs = NewReadySource();
        readyTask = _readyTcs.Task;
        var token = _cts.Token;
        _listenTask = RunAsync(token, _readyTcs);
      }
    }

    await readyTask.WaitAsync(stoppingToken).ConfigureAwait(false);
  }

  private async Task RunAsync(CancellationToken ct, TaskCompletionSource readyTcs)
  {
    try
    {
      var channel = await channelPool.GetAsync(ct).ConfigureAwait(false);
      _channel = channel;

      await using var consumer = new RabbitMqConsumer(channel, _queueName);
      var enumerator = consumer.ConsumeAsync(ct).GetAsyncEnumerator(ct);
      Task<bool>? pendingMoveNext = null;
      try
      {
        // The first MoveNextAsync starts the iterator. The consumer's explicit
        // readiness signal only completes after BasicConsumeAsync succeeds.
        pendingMoveNext = enumerator.MoveNextAsync().AsTask();
        await consumer.WaitUntilReadyAsync().WaitAsync(ct).ConfigureAwait(false);
        readyTcs.TrySetResult();

        while (await pendingMoveNext.ConfigureAwait(false))
        {
          await ReceiveMessageAsync(channel, enumerator.Current, ct).ConfigureAwait(false);
          pendingMoveNext = enumerator.MoveNextAsync().AsTask();
        }

        pendingMoveNext = null;
      }
      finally
      {
        // Async iterators reject concurrent MoveNextAsync/DisposeAsync calls.
        // Drain the outstanding move before disposing the enumerator.
        if (pendingMoveNext is not null)
        {
          try
          {
            await pendingMoveNext.ConfigureAwait(false);
          }
          catch (OperationCanceledException) when (ct.IsCancellationRequested)
          {
          }
          catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException) when (ct.IsCancellationRequested)
          {
          }
        }

        await enumerator.DisposeAsync().ConfigureAwait(false);
      }
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
      readyTcs.TrySetCanceled(ct);
      logger.LogInformation("RabbitMQ listener cancellation requested for {Queue}", _queueName);
    }
    catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ct.IsCancellationRequested)
    {
      readyTcs.TrySetCanceled(ct);
      logger.LogDebug(ex, "RabbitMQ consumer stopped during cancellation for {Queue}", _queueName);
    }
    catch (Exception ex)
    {
      readyTcs.TrySetException(ex);
      logger.LogCritical(ex, "Fatal error in RabbitMQ listener for {Queue}", _queueName);
      throw;
    }
    finally
    {
      var channel = Interlocked.Exchange(ref _channel, null);
      if (channel is not null)
        channelPool.Return(channel);
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
    CancellationTokenSource? cts;
    lock (_lock)
    {
      cts = _cts;
      runningTask = _listenTask;
      cts?.Cancel();
    }

    if (runningTask is not null)
    {
      try
      {
        await runningTask.WaitAsync(cancellationToken).ConfigureAwait(false);
      }
      finally
      {
        lock (_lock)
        {
          if (ReferenceEquals(_cts, cts))
          {
            _cts = null;
            cts?.Dispose();
          }
        }
      }
    }
    else
    {
      cts?.Dispose();
    }
  }

  public async ValueTask DisposeAsync()
  {
    await StopListenAsync().ConfigureAwait(false);
    GC.SuppressFinalize(this);
  }

  private static TaskCompletionSource NewReadySource()
      => new(TaskCreationOptions.RunContinuationsAsynchronously);
}