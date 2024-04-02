using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka.Serialisation
{
  public class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage> where TMessage : Message
  {
    private readonly JsonSerializerSettings _settings; // Optional for custom JSON settings

    public JsonMessageDeserializer(JsonSerializerSettings settings = null) // Optional constructor for settings
    {
      _settings = settings;
    }

    public TMessage Deserialize(string message)
    {
      if (string.IsNullOrEmpty(message))
      {
        return default; // Consider returning a default value for TMessage (optional)
      }

      using (var reader = new JsonTextReader(new StringReader(message))) // Use StringReader directly
      {
        var serializer = JsonSerializer.Create(_settings);
        return serializer.Deserialize<TMessage>(reader);
      }
    }
  }
}
