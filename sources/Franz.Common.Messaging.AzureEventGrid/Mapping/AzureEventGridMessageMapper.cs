#nullable enable
using Azure.Messaging.EventGrid;
using Franz.Common.Errors;
using Franz.Common.Messaging.AzureEventGrid.Constants;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace Franz.Common.Messaging.AzureEventGrid.Mapping;

internal sealed class AzureEventGridMessageMapper
{
  private readonly ILogger<AzureEventGridMessageMapper> _logger;

  public AzureEventGridMessageMapper(ILogger<AzureEventGridMessageMapper> logger)
  {
    _logger = logger;
  }

  public Message ToMessage(EventGridEvent evt)
  {
    if (evt is null)
      throw new TechnicalException("EventGridEvent cannot be null.");

    // If EventGrid ID isn't a valid Guid, the Message constructor will have already
    // generated a fresh Guid v7 for us as a fallback.
    var message = new Message
    {
      MessageType = evt.EventType,
      Body = evt.Data?.ToString()
    };

    if (Guid.TryParse(evt.Id, out var eventGuid))
    {
      message.Id = eventGuid;
    }

    // CORRELATION LOGIC: 
    // We shouldn't necessarily set CorrelationId = evt.Id (which is unique to this event).
    // Instead, try to find a correlation ID in the data bag if it exists.
    if (evt.Data != null)
    {
      try
      {
        // Simple attempt to extract correlation id from common JSON paths
        using var doc = JsonDocument.Parse(evt.Data);
        if (doc.RootElement.TryGetProperty("CorrelationId", out var prop) &&
            Guid.TryParse(prop.GetString(), out var correlationGuid))
        {
          message.CorrelationId = correlationGuid;
        }
      }
      catch { /* Fallback to default spine behavior in Message class */ }
    }

    // Event Grid → Franz transport headers
    message.Headers[AzureEventGridHeaders.EventType] = new StringValues(evt.EventType);

    if (!string.IsNullOrWhiteSpace(evt.Subject))
    {
      message.Headers[AzureEventGridHeaders.Subject] = new StringValues(evt.Subject);
    }

    if (!string.IsNullOrWhiteSpace(evt.Topic))
    {
      message.Headers[AzureEventGridHeaders.Topic] = new StringValues(evt.Topic);
    }

    message.Headers[AzureEventGridHeaders.EventTime] = new StringValues(evt.EventTime.ToString("O"));

    if (!string.IsNullOrWhiteSpace(evt.DataVersion))
    {
      message.Headers[AzureEventGridHeaders.DataVersion] = new StringValues(evt.DataVersion);
    }

    _logger.LogDebug(
        "Mapped EventGridEvent {EventId} ({EventType}) to Franz Message {MessageId} with Correlation {CorrelationId}",
        evt.Id,
        evt.EventType,
        message.Id,
        message.CorrelationId);

    return message;
  }
}