using Azure.Messaging.ServiceBus;
using Franz.Common.Errors;
using Franz.Common.Headers;
using Franz.Common.Messaging.AzureEventBus.Constants;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.AzureEventBus.Mapping;

internal sealed class AzureEventBusMessageMapper : IAzureEventBusMessageMapper
{
  public ServiceBusMessage ToServiceBusMessage(Message message)
  {
    if (message is null)
      throw new ArgumentNullException(nameof(message));

    var sb = new ServiceBusMessage(message.Body)
    {
      MessageId = message.Id,
      CorrelationId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? null
            : message.CorrelationId,
      ContentType = "application/json"
    };

    // Transport headers
    foreach (var header in message.Headers)
    {
      sb.ApplicationProperties[header.Key] = header.Value.ToString();
    }

    // Franz messaging metadata
    if (!string.IsNullOrWhiteSpace(message.MessageType))
    {
      sb.ApplicationProperties[AzureEventBusHeaders.EventType] =
          message.MessageType;
    }

    return sb;
  }

  public Message FromServiceBusMessage(ServiceBusReceivedMessage message)
  {
    if (string.IsNullOrWhiteSpace(message.MessageId))
    {
      throw new TechnicalException(
          "Azure Service Bus message is missing MessageId.");
    }

    var franzMessage = new Message(message.Body.ToString())
    {
      Id = message.MessageId
    };

    if (!string.IsNullOrWhiteSpace(message.CorrelationId))
    {
      franzMessage.CorrelationId = message.CorrelationId;
    }

    // Restore headers
    foreach (var prop in message.ApplicationProperties)
    {
      franzMessage.Headers[prop.Key] =
          new StringValues(prop.Value?.ToString());
    }

    // Restore message type if present
    if (message.ApplicationProperties.TryGetValue(
        AzureEventBusHeaders.EventType, out var eventType))
    {
      franzMessage.MessageType = eventType?.ToString();
    }

    return franzMessage;
  }
}
