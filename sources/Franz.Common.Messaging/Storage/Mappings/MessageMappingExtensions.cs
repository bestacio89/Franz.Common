#nullable enable
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Storage;

public static class MessageMappingExtensions
{
  public static StoredMessage ToStored(this Message message)
  {
    if (message is null)
      throw new ArgumentNullException(nameof(message));

    if (message.Body is null)
      throw new ArgumentNullException(nameof(message.Body));

    return new StoredMessage
    {
      Id = message.Id, // Native Guid v7 assignment
      Body = message.Body,
      CorrelationId = message.CorrelationId, // Native Guid assignment
      MessageType = message.MessageType,

      // Fix: Create a new Dictionary instead of casting ToDictionary
      Headers = message.Headers.ToDictionary(
        kv => kv.Key,
        kv => kv.Value
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Select(v => v!) 
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