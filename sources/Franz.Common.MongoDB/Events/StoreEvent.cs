
using System.Text.Json;

namespace Franz.Common.MongoDB.Events
{
  /// <summary>
  /// Wrapper for persisting domain events in MongoDB.
  /// Contains envelope metadata + serialized payload.
  /// </summary>
  public class StoredEvent
  {
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredOn { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// JSON payload containing the actual event data.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Deserialize payload back into the concrete event type.
    /// </summary>
    public object DeserializePayload(Type targetType)
    {
      return JsonSerializer.Deserialize(Payload, targetType)
             ?? throw new InvalidOperationException($"Failed to deserialize event {EventId} of type {EventType}");
    }
  }
}
