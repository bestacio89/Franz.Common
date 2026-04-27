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

    var message = new Message(stored.Body)
    {
      Id = stored.Id,
      CorrelationId = stored.CorrelationId,
      MessageType = stored.MessageType,
      Properties = (stored.Properties ?? new Dictionary<string, object>())
        .Where(kvp => kvp.Value is not null)
        .ToDictionary(
          kvp => kvp.Key,
          kvp => kvp.Value!,
          StringComparer.OrdinalIgnoreCase)
    };

    if (stored.Headers is not null)
    {
      foreach (var header in stored.Headers)
      {
        var values = header.Value ?? Array.Empty<string>();

        var safeValues = values
          .Where(v => !string.IsNullOrWhiteSpace(v))
          .Select(v => v!)
          .ToArray();

        if (safeValues.Length == 0)
          continue;

        message.Headers[header.Key] = safeValues;
      }
    }

    return message;
  }
}