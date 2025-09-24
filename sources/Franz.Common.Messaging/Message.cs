using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Headers;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging;

public class Message : INotification
{
  public Message() { }
  public string Id { get; set; } = Guid.NewGuid().ToString("N");

  public Message(string? body) => Body = body;

  public Message(string? body, IEnumerable<KeyValuePair<string, StringValues>> headers)
  {
    Body = body;
    Headers = new MessageHeaders(headers);
  }

  public Message(string? body, IEnumerable<KeyValuePair<string, string[]>> headers)
  {
    Body = body;
    Headers = new MessageHeaders(
        headers.Select(header =>
            new KeyValuePair<string, StringValues>(
                header.Key, new StringValues(header.Value)
            )
        )
    );
  }

  /// <summary>
  /// The raw message body (payload). Can be null if the transport didn't carry a body.
  /// </summary>
  public virtual string? Body { get; set; }

  /// <summary>
  /// Transport-level headers (Kafka, HTTP, etc.).
  /// Always initialized to avoid null checks.
  /// </summary>
  public virtual MessageHeaders Headers { get; set; } = new();

  /// <summary>
  /// User-defined metadata for business-level properties that should not be mixed with transport headers.
  /// </summary>
  public virtual IDictionary<string, object> Properties { get; set; } =
      new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Correlation identifier for distributed tracing.
  /// </summary>
  public virtual string CorrelationId
  {
    get => Properties.TryGetValue(nameof(CorrelationId), out var v) ? v.ToString()! : string.Empty;
    set => Properties[nameof(CorrelationId)] = value;
  }

  /// <summary>
  /// Logical message type (used for deserialization).
  /// </summary>
  public virtual string? MessageType
  {
    get => Properties.TryGetValue(nameof(MessageType), out var v) ? v.ToString() : null;
    set => Properties[nameof(MessageType)] = value!;
  }

  public T? GetProperty<T>(string key)
  {
    if (Properties.TryGetValue(key, out var value) && value is T castValue)
      return castValue;
    return default;
  }

  public void SetProperty<T>(string key, T value) => Properties[key] = value!;
}
