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

    var targetTypeInfo = options.GetTypeInfo(target.GetType());
    var result = JsonSerializer.Deserialize(element.GetRawText(), targetTypeInfo);

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

    var typeInfo = options.GetTypeInfo(value.GetType());
    JsonSerializer.Serialize(writer, value, typeInfo);
  }

  protected abstract T Create(Type typeToConvert, JsonElement json);
}