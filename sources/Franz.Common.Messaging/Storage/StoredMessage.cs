#nullable enable
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging;

/// <summary>
/// Persistent representation of a Message. 
/// Optimized for Outbox/Inbox patterns using native Guid v7 for chronological sorting.
/// </summary>
public class StoredMessage
{
  public StoredMessage()
  {
    // Default to v7 to ensure the database record is sortable even if 
    // the higher-level Message object isn't provided immediately.
    Id = Guid.CreateVersion7();
  }

  /// <summary>
  /// Unique identifier for this storage record. Uses Guid v7 for clustered index efficiency.
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// The serialized message payload.
  /// </summary>
  public string Body { get; set; } = default!;

  /// <summary>
  /// Transport-agnostic headers (X-Correlation-ID, etc).
  /// </summary>
  public IDictionary<string, string[]> Headers { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Internal processing properties.
  /// </summary>
  public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// The Golden Thread. Links this message to the original intent/request.
  /// </summary>
  public Guid CorrelationId { get; set; }

  /// <summary>
  /// Assembly-qualified name or semantic name of the message type.
  /// </summary>
  public string? MessageType { get; set; }

  /// <summary>
  /// Precise creation time. Guid v7 also contains this, but an explicit column aids SQL queries.
  /// </summary>
  public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Indicates when the message successfully left the Outbox.
  /// </summary>
  public DateTime? SentOn { get; set; }

  // 🚀 Reliability & Resilience fields
  public int RetryCount { get; set; } = 0;

  public string? LastError { get; set; }

  public DateTime? LastTriedOn { get; set; }

  public bool IsDeadLetter { get; set; } = false;

  /// <summary>
  /// Helper to bridge the storage entity back to the domain Message object.
  /// </summary>
  public Message ToMessage()
  {
    var message = new Message(Body)
    {
      Id = this.Id,
      CorrelationId = this.CorrelationId,
      MessageType = this.MessageType,
      Properties = new Dictionary<string, object>(this.Properties)
    };

    foreach (var header in Headers)
    {
      message.Headers[header.Key] = new StringValues(header.Value);
    }

    return message;
  }
}