#nullable enable

using System.Text.Json;
using Franz.Common.Errors; // TechnicalException
using Franz.Common.Messaging.Kafka.Serialisation;

// If StoredMessage lives elsewhere, keep your existing using.
// using Franz.Common.Messaging.Storage;

namespace Franz.Common.Messaging.Kafka.Serialisation;

public sealed class JsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
  where TMessage : StoredMessage
{
  private readonly JsonSerializerOptions _options;

  public JsonMessageDeserializer(JsonSerializerOptions? options = null)
  {
    // Prefer Franz defaults if caller doesn't override.
    _options = options ?? Franz.Common.Serialization.FranzJson.Default;
  }

  public TMessage Deserialize(string message)
  {
    if (string.IsNullOrWhiteSpace(message))
    {
      throw new TechnicalException(
        $"Cannot deserialize {typeof(TMessage).Name}: input message is null or empty.");
    }

    try
    {
      var result = JsonSerializer.Deserialize<TMessage>(message, _options);

      return result ?? throw new TechnicalException(
        $"Deserialization returned null for {typeof(TMessage).Name}. Message content: {message}");
    }
    catch (JsonException ex)
    {
      throw new TechnicalException(
        $"Failed to deserialize {typeof(TMessage).Name}. Raw message: {message}", ex);
    }
    catch (Exception ex)
    {
      // Optional but useful: keep parity with "anything unexpected becomes TechnicalException"
      throw new TechnicalException(
        $"Unexpected failure while deserializing {typeof(TMessage).Name}. Raw message: {message}", ex);
    }
  }
}
