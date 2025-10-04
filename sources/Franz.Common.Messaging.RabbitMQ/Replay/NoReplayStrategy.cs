using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public class NoReplayStrategy : IReplayStrategy
{
  public const string ReplayHeader = "x-replay";
  public const string DelayHeader = "x-delay";
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";

  private readonly IModelProvider modelProvider;
  private readonly string deadLetterQueueName;

  public NoReplayStrategy(IModelProvider modelProvider, IAssemblyAccessor assemblyAccessor, IOptions<MessagingOptions> messagingOptions)
  {
    this.modelProvider = modelProvider;
    var assembly = assemblyAccessor.GetEntryAssembly();
    deadLetterQueueName = QueueNamer.GetDeadLetterQueueName(assembly);
  }

  public void Replay(BasicDeliverEventArgs basicDeliverEventArgs, Exception ex)
  {
    var basicProperties = modelProvider.Current.CreateBasicProperties();
    basicProperties.DeliveryMode = 2;
    basicProperties.Headers = basicDeliverEventArgs.BasicProperties.Headers;
    basicProperties.Headers ??= new Dictionary<string, object>();

    basicProperties.Headers.Add(ExceptionMessageHeader, ex.Message);
    basicProperties.Headers.Add(ExceptionStackTraceHeader, ex.StackTrace);

    if (!modelProvider.Current.HasTransaction())
      modelProvider.Current.TxSelect();

    modelProvider.Current.BasicPublish(string.Empty, deadLetterQueueName, true, basicProperties, basicDeliverEventArgs.Body);
    modelProvider.Current.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
  }
}
