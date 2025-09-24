using Microsoft.Extensions.Primitives;
using System.Linq;

namespace Franz.Common.Messaging.Storage
{
  public static class MessageMappingExtensions
  {
    public static StoredMessage ToStored(this Message message)
    {
      return new StoredMessage
      {
        Body = message.Body,
        Headers = message.Headers.ToDictionary(
              kv => kv.Key,
              kv => kv.Value.Where(v => v != null).ToArray()! // StringValues -> string[]
          ),
        Properties = new Dictionary<string, object>(message.Properties),
        CorrelationId = message.Headers.TryGetValue("correlation-id", out var cid)
              ? string.Join(",", cid.Where(v => v != null)) // safe flatten
              : null,
        MessageType = message.Properties.TryGetValue("message-type", out var mt)
              ? mt?.ToString()
              : null
      };
    }





    public static Message ToMessage(this StoredMessage stored)
    {
      var msg = new Message(
          stored.Body,
          stored.Headers.Select(h => new KeyValuePair<string, string[]>(h.Key, h.Value))
      )
      {
        Properties = new Dictionary<string, object>(stored.Properties)
      };

      if (!string.IsNullOrEmpty(stored.CorrelationId))
        msg.Headers["correlation-id"] = new[] { stored.CorrelationId };

      if (!string.IsNullOrEmpty(stored.MessageType))
        msg.Properties["message-type"] = stored.MessageType;

      return msg;
    }

  }
}
