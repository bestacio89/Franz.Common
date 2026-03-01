#nullable enable
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Storage;

public static class MessageMappingExtensions
{
  public static StoredMessage ToStored(this Message message)
  {
    if (message is null) throw new ArgumentNullException(nameof(message));
    if (message.Body is null) throw new ArgumentNullException(nameof(message.Body));

    return new StoredMessage
    {
      Id = message.Id,
      Body = message.Body,
      CorrelationId = message.CorrelationId,
      MessageType = message.MessageType,

      // The Fix: Use StringValues.IsNullOrEmpty and filter the entries
      Headers = message.Headers
    .Where(kv => !StringValues.IsNullOrEmpty(kv.Value) &&
                 kv.Value.Any(v => !string.IsNullOrWhiteSpace(v)))
    .ToDictionary(
        kv => kv.Key,
        kv => kv.Value
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!) // The "Dammit Operator" to remove nullability from the string
            .ToArray(),
        StringComparer.OrdinalIgnoreCase
    ),

      Properties = new Dictionary<string, object>(message.Properties, StringComparer.OrdinalIgnoreCase),
      CreatedOn = DateTime.UtcNow
    };
  }


  public static Message ToMessage(this StoredMessage stored)
  {
    if (stored is null)
      throw new ArgumentNullException(nameof(stored));

    // Use the Message constructor that handles body initialization
    var message = new Message(stored.Body)
    {
      Id = stored.Id,
      CorrelationId = stored.CorrelationId,
      MessageType = stored.MessageType,
      Properties = new Dictionary<string, object>(stored.Properties ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase)
    };

    // Restore Headers into MessageHeaders (StringValues)
    if (stored.Headers != null)
    {
      foreach (var header in stored.Headers)
      {
        message.Headers[header.Key] = new StringValues(header.Value);
      }
    }

    return message;
  }
}