using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka.Serialisation;
public class JsonMessageDeserializer : IMessageDeserializer
{
  private readonly JsonSerializerSettings _settings; // Optional for custom JSON settings

  public JsonMessageDeserializer(JsonSerializerSettings settings = null) // Optional constructor for settings
  {
    _settings = settings;
  }

  public object Deserialize(string message)
  {
    if (string.IsNullOrEmpty(message))
    {
      return null; // Handle empty messages (optional)
    }

    using (var reader = new JsonTextReader(new StringReader(message))) // Use JsonTextReader
    {
      var serializer = JsonSerializer.Create(_settings);
      return serializer.Deserialize(reader);
    }
  }
}