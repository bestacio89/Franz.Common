using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public class ProgressiveFourStepReplayStrategy : IReplayStrategy
{
  public const string ReplayHeader = "x-replay";
  public const string DelayHeader = "x-delay";
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";

  private readonly IModelProvider _provider;
  private readonly string _queueName;
  private readonly string _replayExchange;
  private readonly string _deadLetterQueue;

  public ProgressiveFourStepReplayStrategy(
      IModelProvider provider,
      IAssemblyAccessor accessor,
      IOptions<MessagingOptions> options)
  {
    _provider = provider;

    var entry = accessor.GetEntryAssembly();
    _queueName = QueueNamer.GetQueueName(entry);
    _replayExchange = ExchangeNamer.GetReplayExchangeName(entry);
    _deadLetterQueue = QueueNamer.GetDeadLetterQueueName(entry);
  }

  public async Task ReplayAsync(BasicDeliverEventArgs e, Exception ex)
  {
    var channel = _provider.Current;

    var props = new BasicProperties
    {
      DeliveryMode = (DeliveryModes)2,
      Headers = e.BasicProperties.Headers ?? new Dictionary<string, object>()
    };

    // Count replay attempts
    var replayCount = props.Headers.ContainsKey(ReplayHeader)
        ? Convert.ToInt32(props.Headers[ReplayHeader]) + 1
        : 1;

    props.Headers[ReplayHeader] = replayCount;

    // Progressive delays
    var delay = replayCount switch
    {
      1 => 1_000,    // 1s
      2 => 10_000,   // 10s
      3 => 60_000,   // 60s
      _ => 300_000   // 5 min before sending to DLQ
    };

    if (replayCount < 4)
    {
      // Replay to delayed exchange
      props.Headers[DelayHeader] = delay;

      await channel.BasicPublishAsync(
          exchange: _replayExchange,
          routingKey: _queueName,
          mandatory: true,
          basicProperties: props,
          body: e.Body);
    }
    else
    {
      // FINAL attempt → send to DLQ
      props.Headers[ExceptionMessageHeader] = ex.Message;
      props.Headers[ExceptionStackTraceHeader] = ex.StackTrace ?? string.Empty;

      await channel.BasicPublishAsync(
          exchange: "",
          routingKey: _deadLetterQueue,
          mandatory: true,
          basicProperties: props,
          body: e.Body);
    }

    // ACK the original failed message
    await channel.BasicAckAsync(e.DeliveryTag, false);
  }
}
