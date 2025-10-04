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

public class Listener : IListener
{
  private readonly IModelProvider modelProvider;
  private readonly IBasicConsumerFactory basicConsumerFactory;
  private readonly IReplayStrategy? replayStrategy;
  private readonly ILogger<Listener> logger;
  private readonly string queueName;

  private EventingBasicConsumer? consumer;

  public Listener(
      IModelProvider modelProvider,
      IAssemblyAccessor assemblyAccessor,
      IBasicConsumerFactory basicConsumerFactory,
      ILogger<Listener> logger,
      IReplayStrategy? replayStrategy = null)
  {
    this.modelProvider = modelProvider;
    this.basicConsumerFactory = basicConsumerFactory;
    this.logger = logger;
    this.replayStrategy = replayStrategy;

    var assembly = assemblyAccessor.GetEntryAssembly();
    queueName = QueueNamer.GetQueueName(assembly);
  }

  public string? Tag { get; private set; }

  public event EventHandler<MessageEventArgs>? Received;

  public async Task Listen(CancellationToken stoppingToken = default)
  {
    consumer = basicConsumerFactory.Build(modelProvider.Current);

    consumer.Received += ReceiveMessage;

    Tag = modelProvider.Current.BasicConsume(
        queue: queueName,
        autoAck: false,
        consumerTag: string.Empty,
        noLocal: false,
        exclusive: false,
        arguments: null,
        consumer: consumer);

    // Keep the listener alive until cancellation is requested
    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        await Task.Delay(500, stoppingToken); // small delay to yield back to scheduler
      }
    }
    catch (TaskCanceledException)
    {
      // expected when stoppingToken is triggered
    }
  }

  private void ReceiveMessage(object? sender, BasicDeliverEventArgs e)
  {
    try
    {
      var message = new Message
      {
        Headers = TransfertHeaders(e),
        Body = Encoding.UTF8.GetString(e.Body.ToArray(), 0, e.Body.Length),
      };

      Received?.Invoke(this, new MessageEventArgs(message));

      modelProvider.Current.BasicAck(e.DeliveryTag, false);
    }
    catch (Exception ex)
    {
      logger.LogError(ex.GetBaseException(), "Error while processing message");

      if (replayStrategy != null)
      {
        try
        {
          replayStrategy.Replay(e, ex);
        }
        catch (Exception replayException)
        {
          logger.LogError(replayException, "Error while replaying message");

          modelProvider.Current.BasicNack(e.DeliveryTag, false, false);
          throw new AggregateException(ex, replayException);
        }
      }
      else
      {
        modelProvider.Current.BasicNack(e.DeliveryTag, false, false);
      }

      throw;
    }
    finally
    {
      if (modelProvider.Current.HasTransaction())
        modelProvider.Current.TxCommit();
    }
  }

  private static MessageHeaders TransfertHeaders(BasicDeliverEventArgs e)
  {
    var dictionary = e.BasicProperties.Headers?
        .Where(header => !header.Key.StartsWith("x-"))
        .ToDictionary(
            x => x.Key,
            x => new StringValues(Encoding.UTF8.GetString((byte[])x.Value)))
        ?? new Dictionary<string, StringValues>();

    return new MessageHeaders(dictionary);
  }

  public void StopListen()
  {
    if (!string.IsNullOrEmpty(Tag))
    {
      modelProvider.Current.BasicCancel(Tag);
    }

    Tag = null;

    if (consumer != null)
    {
      consumer.Received -= ReceiveMessage;
      consumer = null;
    }
  }
}
