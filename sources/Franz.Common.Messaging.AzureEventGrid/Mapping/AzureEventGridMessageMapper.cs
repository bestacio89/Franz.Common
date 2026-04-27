using Azure.Messaging.EventGrid;
using Franz.Common.Errors;
using Franz.Common.Messaging.AzureEventGrid.Constants;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Logging;
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

    var message = new Message
    {
      MessageType = Normalize(evt.EventType),
      Body = evt.Data?.ToString(),
      Id = TryParseGuid(evt.Id)
    };

    // -----------------------------
    // Correlation (safe nullable handling)
    // -----------------------------
    message.CorrelationId = ExtractCorrelationId(evt);

    // -----------------------------
    // Headers (normalized boundary)
    // -----------------------------
    ApplyHeaders(message, evt);

    // -----------------------------
    // Logging
    // -----------------------------
    _logger.LogDebug(
      "Mapped EventGridEvent {EventId} ({EventType}) → Message {MessageId} (Correlation {CorrelationId})",
      evt.Id,
      evt.EventType,
      message.Id,
      message.CorrelationId);

    return message;
  }

  // =====================================================
  // Header Mapping (transport boundary)
  // =====================================================

  private static void ApplyHeaders(Message message, EventGridEvent evt)
  {
    var headers = message.Headers;

    SetHeader(headers, AzureEventGridHeaders.EventType, evt.EventType);
    SetHeader(headers, AzureEventGridHeaders.Subject, evt.Subject);
    SetHeader(headers, AzureEventGridHeaders.Topic, evt.Topic);
    SetHeader(headers, AzureEventGridHeaders.DataVersion, evt.DataVersion);

    headers[AzureEventGridHeaders.EventTime] =
      new[] { evt.EventTime.ToString("O") };
  }

  private static void SetHeader(
    IDictionary<string, string[]> headers,
    string key,
    string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return;

    headers[key] = new[] { value };
  }

  // =====================================================
  // Correlation Extraction (safe, nullable-first design)
  // =====================================================

  private static Guid? ExtractCorrelationId(EventGridEvent evt)
  {
    if (evt.Data is null)
      return null;

    try
    {
      using var doc = JsonDocument.Parse(evt.Data);

      if (!doc.RootElement.TryGetProperty("CorrelationId", out var prop))
        return null;

      var raw = prop.GetString();

      return Guid.TryParse(raw, out var guid)
        ? guid
        : null;
    }
    catch
    {
      return null; // never fail transport mapping
    }
  }

  // =====================================================
  // Safe parsing helpers
  // =====================================================

  private static Guid TryParseGuid(string? value)
  => Guid.TryParse(value, out var guid) ? guid : Guid.Empty;

  private static string Normalize(string? value)
    => string.IsNullOrWhiteSpace(value) ? string.Empty : value;
}