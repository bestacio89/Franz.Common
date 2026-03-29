#nullable enable
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Headers;
using System.Text.Json.Serialization;

namespace Franz.Common.Messaging.Messages;

/// <summary>
/// Transport-neutral message DTO.
/// Senior Note: Standardized on IDictionary<string, string[]> for high-performance, 
/// serializable headers across Kafka, RabbitMQ, and HTTP.
/// </summary>
public class Message : INotification, IEvent
{
  private Guid _correlationId;

  public Message()
  {
    Id = Guid.CreateVersion7();
    // Set via property to trigger synchronization across Metadata buckets
    CorrelationId = Guid.CreateVersion7();
  }

  public Message(string? messageBody) : this()
  {
    Body = messageBody;
  }

  // SENIOR FIX: Parameter aligned with string[] to resolve CS1503
  public Message(string? body, IDictionary<string, string[]> dictionary) : this(body)
  {
    foreach (var kv in dictionary)
    {
      Headers[kv.Key] = kv.Value;
    }

    SyncCorrelationFromHeaders();
  }

  public Guid Id { get; set; }
  public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
  public virtual string? Body { get; set; }
  public MessageKind Kind { get; set; } = MessageKind.Command;

  // SENIOR FIX: Initialized as MessageHeaders to provide helper methods natively
  public virtual IDictionary<string, string[]> Headers { get; set; } = new MessageHeaders();

  public virtual IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// The Single Source of Truth for Correlation.
  /// Synchronizes across Properties (for internal logic) and Headers (for wire transport).
  /// </summary>
  [JsonIgnore]
  public virtual Guid CorrelationId
  {
    get => _correlationId;
    set
    {
      _correlationId = value;
      Properties[nameof(CorrelationId)] = value;
      // Wrap in array for JSON/Transport compliance
      Headers["X-Correlation-ID"] = [value.ToString()];
    }
  }

  private void SyncCorrelationFromHeaders()
  {
    // Check standard and lowercase variants for cross-platform compatibility
    if (Headers.TryGetValue("X-Correlation-ID", out var values) ||
        Headers.TryGetValue("correlation-id", out values))
    {
      if (values.Length > 0 && Guid.TryParse(values[0], out var cid))
      {
        _correlationId = cid;
        Properties[nameof(CorrelationId)] = cid;
      }
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
}