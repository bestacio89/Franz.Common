#nullable enable
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Headers;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Messages;

public class Message : INotification
{
  private Guid _correlationId;

  public Message()
  {
    Id = Guid.CreateVersion7();
  }

  public Message(string? messageBody) : this()
  {
    Body = messageBody;
  }

  public Message(string? body, IDictionary<string, IReadOnlyCollection<string>> dictionary) : this(body)
  {
    foreach (var kv in dictionary)
      Headers[kv.Key] = new StringValues(kv.Value.ToArray());

    SyncCorrelationFromHeaders();
  }

  public Message(string? body, MessageHeaders headers) : this(body)
  {
    Headers = headers;
    SyncCorrelationFromHeaders();
  }

  /// <summary>
  /// Unique message identifier (Message ID).
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Raw message payload.
  /// </summary>
  public virtual string? Body { get; set; }

  /// <summary>
  /// Semantic kind of message (Command / Query / Event / Fault).
  /// </summary>
  public MessageKind Kind { get; set; } = MessageKind.Command;

  /// <summary>
  /// Transport-agnostic headers.
  /// </summary>
  public virtual MessageHeaders Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Logical metadata for internal processing.
  /// </summary>
  public virtual IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Hardened CorrelationId. Defaults to a new Guid v7 if not provided.
  /// </summary>
  public virtual Guid CorrelationId
  {
    get
    {
      if (_correlationId == Guid.Empty)
      {
        // Check Properties for legacy/deserialization compatibility
        if (Properties.TryGetValue(nameof(CorrelationId), out var value))
        {
          if (value is Guid guidValue) _correlationId = guidValue;
          else if (value is string str && Guid.TryParse(str, out var g)) _correlationId = g;
        }

        // If still empty, generate the sequential v7
        if (_correlationId == Guid.Empty)
        {
          _correlationId = Guid.CreateVersion7();
          Properties[nameof(CorrelationId)] = _correlationId;
        }
      }
      return _correlationId;
    }
    set
    {
      _correlationId = value;
      Properties[nameof(CorrelationId)] = value;
    }
  }

  public virtual string? MessageType
  {
    get => GetProperty<string>(nameof(MessageType));
    set => SetProperty(nameof(MessageType), value!);
  }

  public T? GetProperty<T>(string key)
      => Properties.TryGetValue(key, out var value) && value is T cast ? cast : default;

  public void SetProperty<T>(string key, T value)
      => Properties[key] = value!;

  private void SyncCorrelationFromHeaders()
  {
    if (Headers.TryGetValue("correlation-id", out var values) && Guid.TryParse(values.ToString(), out var cid))
    {
      CorrelationId = cid;
    }
  }
}