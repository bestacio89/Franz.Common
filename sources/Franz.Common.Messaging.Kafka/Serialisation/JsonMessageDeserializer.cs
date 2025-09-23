using Newtonsoft.Json;
using System;
using System.IO;
using Franz.Common.Errors; // for TechnicalException

namespace Franz.Common.Messaging.Kafka.Serialisation
{
  public class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage> where TMessage : Message
  {
    private readonly JsonSerializerSettings? _settings;

    public JsonMessageDeserializer(JsonSerializerSettings? settings = null)
    {
      _settings = settings;
    }

    public TMessage Deserialize(string message)
    {
      if (string.IsNullOrEmpty(message))
      {
        throw new TechnicalException($"Cannot deserialize {typeof(TMessage).Name}: input message is null or empty.");
      }

      try
      {
        using var reader = new JsonTextReader(new StringReader(message));
        var serializer = JsonSerializer.Create(_settings);

        var result = serializer.Deserialize<TMessage>(reader);

        return result ?? throw new TechnicalException(
          $"Deserialization returned null for {typeof(TMessage).Name}. Message content: {message}");
      }
      catch (JsonException ex)
      {
        throw new TechnicalException($"Failed to deserialize {typeof(TMessage).Name}. Raw message: {message}", ex);
      }
    }
  }
}
