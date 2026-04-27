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

    var sb = new ServiceBusMessage(message.Body)
    {
      MessageId = message.Id.ToString(),
      CorrelationId = message.CorrelationId.ToString(),
      ContentType = "application/json"
    };

    ApplyOutboundProperties(sb, message);

    return sb;
  }

  // =====================================================
  // OUTBOUND (Message → ServiceBus)
  // =====================================================

  private static void ApplyOutboundProperties(
    ServiceBusMessage sb,
    Message message)
  {
    foreach (var header in message.Headers)
    {
      sb.ApplicationProperties[header.Key] =
        header.Value is null || header.Value.Length == 0
          ? string.Empty
          : string.Join(",", header.Value);
    }

    // CorrelationId is NON-nullable Guid in your model
    sb.ApplicationProperties[nameof(Message.CorrelationId)] =
      message.CorrelationId.ToString();

    if (!string.IsNullOrWhiteSpace(message.MessageType))
    {
      sb.ApplicationProperties[AzureEventBusHeaders.EventType] =
        message.MessageType;
    }
  }

  // =====================================================
  // INBOUND (ServiceBus → Message)
  // =====================================================

  public Message FromServiceBusMessage(ServiceBusReceivedMessage message)
  {
    if (string.IsNullOrWhiteSpace(message.MessageId) ||
        !Guid.TryParse(message.MessageId, out var messageGuid))
    {
      throw new TechnicalException(
        $"Azure Service Bus message is missing or has an invalid MessageId: {message.MessageId}");
    }

    var franzMessage = new Message(message.Body.ToString())
    {
      Id = messageGuid,
      CorrelationId = ResolveCorrelationId(message)
    };

    ApplyInboundHeaders(franzMessage, message);

    return franzMessage;
  }

  // =====================================================
  // HEADER RESTORATION
  // =====================================================

  private static void ApplyInboundHeaders(
    Message franzMessage,
    ServiceBusReceivedMessage message)
  {
    foreach (var prop in message.ApplicationProperties)
    {
      franzMessage.Headers[prop.Key] =
        new[] { prop.Value?.ToString() ?? string.Empty };
    }

    if (message.ApplicationProperties.TryGetValue(AzureEventBusHeaders.EventType, out var eventType))
    {
      franzMessage.MessageType = eventType?.ToString();
    }
  }

  // =====================================================
  // CORRELATION RESOLUTION (Guid-based model)
  // =====================================================

  private static Guid ResolveCorrelationId(ServiceBusReceivedMessage message)
  {
    // 1. Native Service Bus correlation id
    if (!string.IsNullOrWhiteSpace(message.CorrelationId) &&
        Guid.TryParse(message.CorrelationId, out var direct))
    {
      return direct;
    }

    // 2. Fallback from application properties
    if (message.ApplicationProperties.TryGetValue(nameof(Message.CorrelationId), out var prop) &&
        Guid.TryParse(prop?.ToString(), out var fallback))
    {
      return fallback;
    }

    // 3. Hard guarantee (your domain model requires Guid)
    return Guid.Empty;
  }
}