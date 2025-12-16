using Azure.Messaging.EventGrid;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.AzureEventGrid.Constants;
using Microsoft.Extensions.Logging;

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

    var message = new Message
    {
      Id = evt.Id,
      MessageType = evt.EventType,
      CorrelationId = evt.Id
    };

    // Event Grid metadata headers (transport dialect)
    message.Headers[AzureEventGridHeaders.EventType] = evt.EventType;

    if (!string.IsNullOrWhiteSpace(evt.Subject))
      message.Headers[AzureEventGridHeaders.Subject] = evt.Subject;

    if (!string.IsNullOrWhiteSpace(evt.Topic))
      message.Headers[AzureEventGridHeaders.Topic] = evt.Topic;

    message.Headers[AzureEventGridHeaders.EventTime] = evt.EventTime.ToString("O");

    if (!string.IsNullOrWhiteSpace(evt.DataVersion))
      message.Headers[AzureEventGridHeaders.DataVersion] = evt.DataVersion;

    // Payload stays opaque
    message.Body = evt.Data.ToString();

    _logger.LogDebug(
        "Mapped EventGridEvent {EventId} ({EventType}) to Franz Message",
        evt.Id,
        evt.EventType);

    return message;
  }
}
