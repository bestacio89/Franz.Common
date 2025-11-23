using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public class NoReplayStrategy : IReplayStrategy
{
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";

  private readonly IModelProvider _provider;
  private readonly string _deadLetterQueue;

  public NoReplayStrategy(
      IModelProvider provider,
      IAssemblyAccessor accessor,
      IOptions<MessagingOptions> options)
  {
    _provider = provider;
    var entry = accessor.GetEntryAssembly();
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

    props.Headers[ExceptionMessageHeader] = ex.Message;
    props.Headers[ExceptionStackTraceHeader] = ex.StackTrace ?? string.Empty;

    await channel.BasicPublishAsync(
        exchange: "",
        routingKey: _deadLetterQueue,
        mandatory: true,
        basicProperties: props,
        body: e.Body);

    // Just ACK the failed message
    await channel.BasicAckAsync(e.DeliveryTag, false);
  }
}
