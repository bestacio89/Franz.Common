using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Franz.Common.Errors;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.AzureEventGrid.Configuration;
using Franz.Common.Messaging.AzureEventGrid.Constants;
using Franz.Common.Messaging.AzureEventGrid.Logging;
using Franz.Common.Messaging.AzureEventGrid.Mapping;
using Franz.Common.Messaging.AzureEventGrid.Models;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.AzureEventGrid.Ingress;

/// <summary>
/// Host-agnostic ingress for Azure Event Grid.
/// Detects subscription validation, applies filtering,
/// maps events to Franz messages, and dispatches via mediator.
/// </summary>
internal sealed class AzureEventGridIngress : IAzureEventGridIngress
{
  private readonly AzureEventGridMessageMapper _mapper;
  private readonly AzureEventGridFilterOptions _filterOptions;
  private readonly IDispatcher _dispatcher;
  private readonly ILogger<AzureEventGridIngress> _logger;

  public AzureEventGridIngress(
      AzureEventGridMessageMapper mapper,
      AzureEventGridFilterOptions filterOptions,
      IDispatcher dispatcher,
      ILogger<AzureEventGridIngress> logger)
  {
    _mapper = mapper;
    _filterOptions = filterOptions;
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task<SubscriptionValidationResult?> IngestAsync(
      EventGridEvent evt,
      CancellationToken cancellationToken = default)
  {
    if (evt == null)
      throw new TechnicalException("EventGridEvent cannot be null.");

    // 🔐 Subscription validation (short-circuit)
    if (evt.EventType == AzureEventGridEventTypes.SubscriptionValidation)
    {
      var data = evt.Data.ToObjectFromJson<SubscriptionValidationEventData>();

      _logger.LogInformation(
          "🔐 Event Grid subscription validation received. ValidationCode={ValidationCode}",
          data.ValidationCode);

      return new SubscriptionValidationResult(data.ValidationCode);
    }

    // 🚫 Event-type filtering
    if (!_filterOptions.IsAllowed(evt.EventType))
    {
      _logger.LogWarning(
          "🚫 EventGridEvent {EventId} of type {EventType} rejected by filter",
          evt.Id,
          evt.EventType);

      return null;
    }

    using (_logger.BeginScope(evt))
    {
      _logger.LogInformation(
          "📥 Ingesting EventGridEvent {EventId} ({EventType})",
          evt.Id,
          evt.EventType);

      var message = _mapper.ToMessage(evt);

      // 🔥 Franz boundary: transport → mediator
      await _dispatcher.PublishNotificationAsync(message, cancellationToken);
    }

    return null;
  }

  public async Task IngestAsync(
      IEnumerable<EventGridEvent> events,
      CancellationToken cancellationToken = default)
  {
    foreach (var evt in events)
    {
      await IngestAsync(evt, cancellationToken);
    }
  }
}
