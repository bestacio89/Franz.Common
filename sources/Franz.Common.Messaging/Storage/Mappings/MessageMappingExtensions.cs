using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Messaging.Storage
{
  public static class MessageMappingExtensions
  {
    public static StoredMessage ToStored(this Message message)
    {
      if (message is null)
        throw new ArgumentNullException(nameof(message));

      if (message.Body is null)
        throw new ArgumentNullException(nameof(message.Body));

      if (message.Properties is null)
        throw new ArgumentNullException(nameof(message.Properties));

      return new StoredMessage
      {
        Body = message.Body,

        // Franz MessageHeaders -> Storage-safe headers
        Headers = (IDictionary<string, string[]>)message.Headers.ToDictionary(
          kv => kv.Key,
          kv => (IReadOnlyCollection<string>)kv.Value
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray()
        ),

        Properties = new Dictionary<string, object>(message.Properties),

        CorrelationId = message.Headers.TryGetValue("correlation-id", out var cid)
          ? string.Join(",", cid.Where(v => !string.IsNullOrWhiteSpace(v)))
          : null,

        MessageType = message.Properties.TryGetValue("message-type", out var mt)
          ? mt?.ToString()
          : null
      };
    }

    public static Message ToMessage(this StoredMessage stored)
    {
      if (stored is null)
        throw new ArgumentNullException(nameof(stored));

      if (stored.Properties is null)
        throw new ArgumentNullException(nameof(stored.Properties));

      var message = new Message(
        stored.Body,
        (IDictionary<string, IReadOnlyCollection<string>>)stored.Headers.Select(h =>
          new KeyValuePair<string, StringValues>(
            h.Key,
            new StringValues(h.Value?.ToArray() ?? Array.Empty<string>())
          )
        )
      )
      {
        Properties = stored.Properties
      };

      // Restore Franz invariants if present
      if (!string.IsNullOrWhiteSpace(stored.CorrelationId))
      {
        message.Headers["correlation-id"] =
          new StringValues(stored.CorrelationId);
      }

      if (!string.IsNullOrWhiteSpace(stored.MessageType))
      {
        message.Properties["message-type"] = stored.MessageType;
      }

      return message;
    }
  }
}
