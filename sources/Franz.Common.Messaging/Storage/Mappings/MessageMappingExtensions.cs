using Microsoft.Extensions.Primitives;
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
        Headers = message.Headers.ToDictionary(
              kv => kv.Key,
              kv => kv.Value
                      .Where(v => !string.IsNullOrEmpty(v))
                      .Select(v => v!)
                      .ToArray()
          ),
        Properties = new Dictionary<string, object>(message.Properties),
        CorrelationId = message.Headers.TryGetValue("correlation-id", out var cid)
              ? string.Join(",", cid.Where(v => v is not null))
              : null,
        MessageType = message.Properties.TryGetValue("message-type", out var mt)
              ? mt?.ToString()
              : null
      };
    }



    public static Message ToMessage(this StoredMessage stored)
    {
      if (stored.Properties is null)
      {
        throw new ArgumentNullException(nameof(stored.Properties));
      }
      var msg = new Message(
          stored.Body,
          stored.Headers.Select(h =>
              new KeyValuePair<string, string[]>(h.Key, h.Value ?? Array.Empty<string>()))
      )
      {
        Properties = stored.Properties
      };

      if (!string.IsNullOrEmpty(stored.CorrelationId))
        msg.Headers["correlation-id"] = new[] { stored.CorrelationId };

      if (!string.IsNullOrEmpty(stored.MessageType))
        msg.Properties["message-type"] = stored.MessageType;

      return msg;
    }

  }
}
