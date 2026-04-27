#nullable enable

using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging;

public class StoredMessage
{
  public StoredMessage()
  {
    Id = Guid.CreateVersion7();
  }

  public Guid Id { get; set; }

  public string Body { get; set; } = string.Empty;

  public IDictionary<string, string[]> Headers { get; set; }
    = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

  public IDictionary<string, object> Properties { get; set; }
    = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

  public Guid? CorrelationId { get; set; }

  public string? MessageType { get; set; }

  public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

  public DateTime? SentOn { get; set; }

  public int RetryCount { get; set; } = 0;

  public string? LastError { get; set; }

  public DateTime? LastTriedOn { get; set; }

  public bool IsDeadLetter { get; set; } = false;

  // =====================================================
  // DOMAIN MAPPING
  // =====================================================

  public Message ToMessage()
  {
    var message = new Message(Body)
    {
      Id = Id,
      CorrelationId = CorrelationId,
      MessageType = MessageType,
      Properties = new Dictionary<string, object>(Properties)
    };

    foreach (var header in Headers)
    {
      message.Headers[header.Key] =
        header.Value is null || header.Value.Length == 0
          ? Array.Empty<string>()
          : header.Value.Where(v => v is not null).ToArray();
    }

    return message;
  }
}