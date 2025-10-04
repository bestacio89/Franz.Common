using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public class ProgressiveFourStepReplayStrategy : IReplayStrategy
{
  public const string ReplayHeader = "x-replay";
  public const string DelayHeader = "x-delay";
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";

  private readonly IModelProvider modelProvider;
  private readonly string queueName;
  private readonly string replayExchangeName;
  private readonly string deadLetterQueueName;

  public ProgressiveFourStepReplayStrategy(IModelProvider modelProvider, IAssemblyAccessor assemblyAccessor, IOptions<MessagingOptions> messagingOptions)
  {
    this.modelProvider = modelProvider;
    var assembly = assemblyAccessor.GetEntryAssembly();
    queueName = QueueNamer.GetQueueName(assembly);
    replayExchangeName = ExchangeNamer.GetReplayExchangeName(assembly);
    deadLetterQueueName = QueueNamer.GetDeadLetterQueueName(assembly);
  }

  public void Replay(BasicDeliverEventArgs basicDeliverEventArgs, Exception ex)
  {
    var basicProperties = modelProvider.Current.CreateBasicProperties();
    basicProperties.DeliveryMode = 2;
    basicProperties.Headers = basicDeliverEventArgs.BasicProperties.Headers;
    basicProperties.Headers ??= new Dictionary<string, object>();

    var replay = 0;
    if (basicProperties.Headers.ContainsKey(ReplayHeader))
      replay = Convert.ToInt32(basicProperties.Headers[ReplayHeader]);
    else
      basicProperties.Headers.Add(ReplayHeader, replay);
    replay++;

    var timeDelay = replay switch
    {
      1 => 1,
      2 => 10,
      3 => 60,
      _ => 300,
    } * 1000;

    if (replay < 5)
    {
      basicProperties.Headers[ReplayHeader] = replay;
      if (basicProperties.Headers.ContainsKey(DelayHeader))
        basicProperties.Headers[DelayHeader] = timeDelay;
      else
        basicProperties.Headers.Add(DelayHeader, timeDelay);

      modelProvider.Current.BasicPublish(replayExchangeName, queueName, true, basicProperties, basicDeliverEventArgs.Body);
    }
    else
    {
      basicProperties.Headers.Add(ExceptionMessageHeader, ex.Message);
      basicProperties.Headers.Add(ExceptionStackTraceHeader, ex.StackTrace);
      modelProvider.Current.BasicPublish(string.Empty, deadLetterQueueName, true, basicProperties, basicDeliverEventArgs.Body);
    }

    modelProvider.Current.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
  }
}
