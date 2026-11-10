using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Factories;

public sealed class ExecutionFaultMessageBuilderStrategy : IMessageBuilderStrategy
{
  private readonly IMessageSerializer _serializer;

  public ExecutionFaultMessageBuilderStrategy()
      : this(new JsonMessageSerializer())
  {
  }

  public ExecutionFaultMessageBuilderStrategy(IMessageSerializer serializer)
      => _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

  public bool CanBuild(object value) => value is Exception or IExecutionFault;

  public Message Build(object value)
  {
    ArgumentNullException.ThrowIfNull(value);
    var fault = value switch
    {
      IExecutionFault executionFault => executionFault,
      Exception exception => ExecutionFault.FromException(exception),
      _ => throw new InvalidOperationException(
          $"Type '{value.GetType().FullName}' cannot be converted to an execution fault.")
    };

    var body = _serializer.Serialize(fault, fault.GetType());
    var message = new Message(body)
    {
      Body = body,
      Kind = MessageKind.Fault,
      MessageType = nameof(ExecutionFault)
    };

    message.Headers.Add(MessagingConstants.ClassName, new[] { nameof(ExecutionFault) });
    message.Headers.Add(MessagingConstants.FaultCode, new[] { fault.Code });

    return message;
  }
}
