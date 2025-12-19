using Franz.Common.Mediator.Messages;

namespace Franz.Common.Messaging;

public class Message : INotification
{
  public Message() { }

  public Message(string? body)
  {
    Body = body;
  }

  public Message(
    string? body,
    IDictionary<string, IReadOnlyCollection<string>> headers)
  {
    Body = body;
    Headers = headers;
  }

  /// <summary>
  /// Unique message identifier.
  /// </summary>
  public string Id { get; set; } = Guid.NewGuid().ToString("N");

  /// <summary>
  /// Raw message payload.
  /// </summary>
  public virtual string? Body { get; set; }

  /// <summary>
  /// Transport-agnostic headers.
  /// These are mapped by each transport adapter (Kafka, Azure, HTTP).
  /// </summary>
  public virtual IDictionary<string, IReadOnlyCollection<string>> Headers { get; set; }
    = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Logical metadata not intended for transport headers.
  /// </summary>
  public virtual IDictionary<string, object> Properties { get; set; }
    = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Correlation identifier for distributed tracing.
  /// </summary>
  public virtual string CorrelationId
  {
    get => Properties.TryGetValue(nameof(CorrelationId), out var v) ? v.ToString()! : string.Empty;
    set => Properties[nameof(CorrelationId)] = value;
  }

  /// <summary>
  /// Logical message type.
  /// </summary>
  public virtual string? MessageType
  {
    get => Properties.TryGetValue(nameof(MessageType), out var v) ? v.ToString() : null;
    set => Properties[nameof(MessageType)] = value!;
  }

  public T? GetProperty<T>(string key)
    => Properties.TryGetValue(key, out var value) && value is T cast
        ? cast
        : default;

  public void SetProperty<T>(string key, T value)
    => Properties[key] = value!;
}
