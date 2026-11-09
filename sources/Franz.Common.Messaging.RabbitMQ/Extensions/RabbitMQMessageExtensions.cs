#nullable enable

using Franz.Common.Messaging.Messages;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Extensions;

/// <summary>
/// Maps the complete transport-neutral message envelope to RabbitMQ properties.
/// </summary>
public static class RabbitMQMessageExtensions
{
  public static BasicProperties ToBasicProperties(this Message message)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (message.Id == Guid.Empty)
      throw new Franz.Common.Messaging.Exceptions.PermanentMessagingException(
          "RabbitMQ messages must have a non-empty message identity before publication.");

    var properties = message.Headers.ToBasicProperties();
    properties.MessageId = message.Id.ToString("D");

    if (message.CorrelationId.HasValue)
      properties.CorrelationId = message.CorrelationId.Value.ToString("D");

    return properties;
  }
}
