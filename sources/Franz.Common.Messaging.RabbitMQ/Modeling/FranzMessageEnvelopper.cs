using System.Text.Json.Serialization;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public sealed record FranzMessageEnvelope<T>(
    Guid MessageId,
    DateTimeOffset Timestamp,
    string MessageType,
    IDictionary<string, object>? Metadata,
    T Payload)
{
  public static FranzMessageEnvelope<T> Create(
      T payload,
      IDictionary<string, object>? metadata = null)
  {
    return new FranzMessageEnvelope<T>(
        MessageId: Guid.NewGuid(),
        Timestamp: DateTimeOffset.UtcNow,
        MessageType: typeof(T).FullName!,
        Metadata: metadata,
        Payload: payload
    );
  }
}
