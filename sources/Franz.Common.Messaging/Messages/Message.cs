#nullable enable

using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Headers;
using System.Text.Json.Serialization;

namespace Franz.Common.Messaging.Messages;

public class Message : INotification, IEvent
{
  private Guid _correlationId;

  public const string CorrelationHeader = "x-correlation-id";

  public Message()
  {
    Id = Guid.CreateVersion7();

    // Default: correlation starts aligned with message identity
    _correlationId = Id;

    SyncProjection();
  }

  public Message(string? body) : this()
  {
    Body = body;
  }

  /// <summary>
  /// Explicit hydration constructor (transport -> domain)
  /// </summary>
  public Message(string? body, IDictionary<string, string[]> headers) : this(body)
  {
    foreach (var kv in headers)
      Headers[kv.Key] = kv.Value;

    HydrateFromHeaders(headers);
  }

  // ---------------------------
  // CORE IDENTITY
  // ---------------------------

  public Guid Id { get; set; }

  public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;

  public virtual string? Body { get; set; }

  public MessageKind Kind { get; set; } = MessageKind.Command;

  // ---------------------------
  // METADATA LAYERS
  // ---------------------------

  public virtual IDictionary<string, string[]> Headers { get; set; }
      = new MessageHeaders();

  public virtual IDictionary<string, object> Properties { get; set; }
      = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  // ---------------------------
  // CORRELATION (SINGLE SOURCE OF TRUTH)
  // ---------------------------

  [JsonIgnore]
  public virtual Guid CorrelationId
  {
    get => _correlationId;
    set
    {
      _correlationId = value;

      Properties[nameof(CorrelationId)] = value;

      SyncProjection();
    }
  }

  // ---------------------------
  // MESSAGE TYPE (SAFE PROPERTY BACKING)
  // ---------------------------

  public virtual string? MessageType
  {
    get => GetProperty<string>(nameof(MessageType));
    set => SetProperty(nameof(MessageType), value);
  }

  // ---------------------------
  // PUBLIC API
  // ---------------------------

  public T? GetProperty<T>(string key)
      => Properties.TryGetValue(key, out var value) && value is T cast
          ? cast
          : default;

  public void SetProperty<T>(string key, T value)
      => Properties[key] = value!;

  // ---------------------------
  // INTERNAL PROJECTIONS
  // ---------------------------

  private void SyncProjection()
  {
    Headers[CorrelationHeader] = new[] { _correlationId.ToString() };
  }

  private void HydrateFromHeaders(IDictionary<string, string[]> headers)
  {
    if (headers.TryGetValue(CorrelationHeader, out var values) &&
        values.Length > 0 &&
        Guid.TryParse(values[0], out var cid))
    {
      _correlationId = cid;
      Properties[nameof(CorrelationId)] = cid;
    }
  }
}