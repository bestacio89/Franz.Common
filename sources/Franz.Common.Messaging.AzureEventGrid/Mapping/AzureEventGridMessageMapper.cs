using Azure.Messaging.EventGrid;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.AzureEventGrid.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.AzureEventGrid.Mapping;

internal sealed class AzureEventGridMessageMapper
{
  private readonly ILogger<AzureEventGridMessageMapper> _logger;

  public AzureEventGridMessageMapper(
    ILogger<AzureEventGridMessageMapper> logger)
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
      CorrelationId = evt.Id,
      Body = evt.Data?.ToString()
    };

    // Event Grid → Franz transport headers
    message.Headers[AzureEventGridHeaders.EventType] =
      new StringValues(evt.EventType);

    if (!string.IsNullOrWhiteSpace(evt.Subject))
    {
      message.Headers[AzureEventGridHeaders.Subject] =
        new StringValues(evt.Subject);
    }

    if (!string.IsNullOrWhiteSpace(evt.Topic))
    {
      message.Headers[AzureEventGridHeaders.Topic] =
        new StringValues(evt.Topic);
    }

    message.Headers[AzureEventGridHeaders.EventTime] =
      new StringValues(evt.EventTime.ToString("O"));

    if (!string.IsNullOrWhiteSpace(evt.DataVersion))
    {
      message.Headers[AzureEventGridHeaders.DataVersion] =
        new StringValues(evt.DataVersion);
    }

    _logger.LogDebug(
      "Mapped EventGridEvent {EventId} ({EventType}) to Franz Message",
      evt.Id,
      evt.EventType);

    return message;
  }
}
