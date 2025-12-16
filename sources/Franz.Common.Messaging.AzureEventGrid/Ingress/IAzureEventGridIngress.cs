using Azure.Messaging.EventGrid;
using Franz.Common.Messaging.AzureEventGrid.Models;

public interface IAzureEventGridIngress
{
  Task<SubscriptionValidationResult?> IngestAsync(
      EventGridEvent eventGridEvent,
      CancellationToken cancellationToken = default);

  Task IngestAsync(
      IEnumerable<EventGridEvent> events,
      CancellationToken cancellationToken = default);
}
