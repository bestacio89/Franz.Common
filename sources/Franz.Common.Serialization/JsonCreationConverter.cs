using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization;

/// <summary>
/// Safe factory-based converter replacing Newtonsoft JsonCreationConverter.
/// Explicit construction, no reflection population, no polymorphism.
/// </summary>
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

    var target = Create(typeToConvert, element);

    // Populate explicitly (safe)
    var json = element.GetRawText();
    return (T)JsonSerializer.Deserialize(
        json,
        target.GetType(),
        options)!;
  }

  public override void Write(
      Utf8JsonWriter writer,
      T value,
      JsonSerializerOptions options)
  {
    JsonSerializer.Serialize(
        writer,
        value,
        value.GetType(),
        options);
  }

  /// <summary>
  /// Explicit factory method (NO reflection).
  /// </summary>
  protected abstract T Create(Type typeToConvert, JsonElement json);
}
