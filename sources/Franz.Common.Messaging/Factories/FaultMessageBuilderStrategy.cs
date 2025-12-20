using Franz.Common.Messaging.Messages;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace Franz.Common.Messaging.Factories;

public sealed class ExecutionFaultMessageBuilderStrategy
    : IMessageBuilderStrategy
{
  public bool CanBuild(object value)
      => value is Exception || value is IExecutionFault;

  public Message Build(object value)
  {
    var fault = value switch
    {
      IExecutionFault f => f,
      Exception ex => ExecutionFault.FromException(ex),
      _ => throw new InvalidOperationException()
    };

    var body = JsonSerializer.Serialize(fault, FranzJson.Default);

    var message = new Message(body);

    message.Headers.Add(
        MessagingConstants.ClassName,
        new StringValues(nameof(ExecutionFault)));

    message.Headers.Add(
        MessagingConstants.FaultCode,
        new StringValues(fault.Code));

    return message;
  }
}
