#nullable enable
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

    // ASB accepts strings for these, but since our Message class ensures
    // v7 on init, we pass the clean Guid strings.
    var sb = new ServiceBusMessage(message.Body)
    {
      MessageId = message.Id.ToString(),
      CorrelationId = message.CorrelationId.ToString(),
      ContentType = "application/json"
    };

    // Transport headers
    foreach (var header in message.Headers)
    {
      sb.ApplicationProperties[header.Key] = header.Value.ToString();
    }

    // Franz messaging metadata - Explicitly ensuring CorrelationId is in the property bag too
    sb.ApplicationProperties[nameof(Message.CorrelationId)] = message.CorrelationId.ToString();

    if (!string.IsNullOrWhiteSpace(message.MessageType))
    {
      sb.ApplicationProperties[AzureEventBusHeaders.EventType] = message.MessageType;
    }

    return sb;
  }

  public Message FromServiceBusMessage(ServiceBusReceivedMessage message)
  {
    // ASB MessageId is a string, but we expect a Guid v7 string from our system
    if (string.IsNullOrWhiteSpace(message.MessageId) || !Guid.TryParse(message.MessageId, out var messageGuid))
    {
      throw new TechnicalException($"Azure Service Bus message is missing or has an invalid MessageId: {message.MessageId}");
    }

    var franzMessage = new Message(message.Body.ToString())
    {
      Id = messageGuid
    };

    // Restore CorrelationId from native ASB property
    if (!string.IsNullOrWhiteSpace(message.CorrelationId) && Guid.TryParse(message.CorrelationId, out var correlationGuid))
    {
      franzMessage.CorrelationId = correlationGuid;
    }
    // Fallback to ApplicationProperties if native CorrelationId was stripped or missing
    else if (message.ApplicationProperties.TryGetValue(nameof(Message.CorrelationId), out var propCorrId)
             && Guid.TryParse(propCorrId?.ToString(), out var propGuid))
    {
      franzMessage.CorrelationId = propGuid;
    }

    // Restore headers
    foreach (var prop in message.ApplicationProperties)
    {
      franzMessage.Headers[prop.Key] = new StringValues(prop.Value?.ToString());
    }

    // Restore message type
    if (message.ApplicationProperties.TryGetValue(AzureEventBusHeaders.EventType, out var eventType))
    {
      franzMessage.MessageType = eventType?.ToString();
    }

    return franzMessage;
  }
}