
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Franz.Common.MongoDB.Events
{
  /// <summary>
  /// Wrapper for persisting domain events in MongoDB.
  /// Contains envelope metadata + serialized payload.
  /// </summary>
  public class StoredEvent
  {
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid EventId { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid AggregateId { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? TenantId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredOn { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid CorrelationId { get; set; } = Guid.Empty;

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
