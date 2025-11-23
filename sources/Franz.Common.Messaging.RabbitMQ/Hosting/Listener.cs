using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Replay;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ.Hosting;

public sealed class Listener : IListener, IAsyncDisposable
{
  private readonly IModelProvider _modelProvider;
  private readonly IBasicConsumerFactory _consumerFactory;
  private readonly IReplayStrategy? _replayStrategy;
  private readonly ILogger<Listener> _logger;
  private readonly string _queueName;

  private AsyncEventingBasicConsumer? _consumer;
  private string? _tag;

  public event EventHandler<MessageEventArgs>? Received;

  public Listener(
      IModelProvider modelProvider,
      IAssemblyAccessor assemblyAccessor,
      IBasicConsumerFactory consumerFactory,
      ILogger<Listener> logger,
      IReplayStrategy? replayStrategy = null)
  {
    _modelProvider = modelProvider;
    _consumerFactory = consumerFactory;
    _logger = logger;
    _replayStrategy = replayStrategy;

    var assembly = assemblyAccessor.GetEntryAssembly();
    _queueName = QueueNamer.GetQueueName(assembly);
  }

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    var channel = _modelProvider.Current;

    _consumer = _consumerFactory.BuildAsync(channel);
    _consumer.ReceivedAsync += ReceiveMessageAsync;

    _tag = await channel.BasicConsumeAsync(
        queue: _queueName,
        autoAck: false,
        consumer: _consumer,
        cancellationToken: stoppingToken);

    _logger.LogInformation("RabbitMQ Listener started on queue {Queue}", _queueName);

    try
    {
      await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    catch (TaskCanceledException)
    {
      // normal shutdown
    }
  }

  private async Task ReceiveMessageAsync(object? sender, BasicDeliverEventArgs e)
  {
    try
    {
      var message = new Message
      {
        Headers = ExtractHeaders(e),
        Body = Encoding.UTF8.GetString(e.Body.ToArray())
      };

      Received?.Invoke(this, new MessageEventArgs(message));

      await _modelProvider.Current.BasicAckAsync(e.DeliveryTag, false);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while processing message");

      if (_replayStrategy != null)
      {
        try
        {
          await _replayStrategy.ReplayAsync(e, ex);
        }
        catch (Exception replayEx)
        {
          _logger.LogError(replayEx, "Replay failed");
          await _modelProvider.Current.BasicNackAsync(e.DeliveryTag, false, false);
          throw new AggregateException(ex, replayEx);
        }
      }
      else
      {
        await _modelProvider.Current.BasicNackAsync(e.DeliveryTag, false, false);
      }

      throw;
    }
  }

  private static MessageHeaders ExtractHeaders(BasicDeliverEventArgs e)
  {
    var headers = e.BasicProperties.Headers?
        .Where(h => !h.Key.StartsWith("x-"))
        .ToDictionary(
            h => h.Key,
            h => new StringValues(Encoding.UTF8.GetString((byte[])h.Value)))
        ?? new Dictionary<string, StringValues>();

    return new MessageHeaders(headers);
  }

  // REQUIRED by IListener
  public void StopListen()
  {
    // fire and forget — force sync wrapper around async
    StopListenAsync().Wait();
  }

  public async Task StopListenAsync()
  {
    if (_tag != null)
    {
      await _modelProvider.Current.BasicCancelAsync(_tag);
      _tag = null;
    }

    if (_consumer != null)
    {
      _consumer.ReceivedAsync -= ReceiveMessageAsync;
      _consumer = null;
    }
  }

  // REQUIRED by IAsyncDisposable
  public async ValueTask DisposeAsync()
  {
    await StopListenAsync();
  }
}
