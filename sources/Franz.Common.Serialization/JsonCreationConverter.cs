using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization;

public abstract class SystemTextCreationConverter<T> : JsonConverter<T>
{
  public override bool CanConvert(Type typeToConvert)
      => typeof(T).IsAssignableFrom(typeToConvert);

  public override T Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options)
  {
    using var document = JsonDocument.ParseValue(ref reader);
    var element = document.RootElement;

    var target = Create(typeToConvert, element)
        ?? throw new JsonException($"Factory returned null for {typeof(T).FullName}");

    var json = element.GetRawText();

    var result = JsonSerializer.Deserialize(
        json,
        target.GetType(),
        options);

    if (result is null)
      throw new JsonException($"Deserialization returned null for {target.GetType().FullName}");

    return (T)result;
  }

  public override void Write(
      Utf8JsonWriter writer,
      T value,
      JsonSerializerOptions options)
  {
    if (value is null)
      throw new JsonException("Cannot serialize null value.");

    JsonSerializer.Serialize(
        writer,
        value,
        value.GetType(),
        options);
  }

  protected abstract T Create(Type typeToConvert, JsonElement json);
}