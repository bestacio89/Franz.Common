using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Messaging.Serialization;

public class JsonMessageSerializer(JsonSerializerContext? jsonSerializerContext = null) : IMessageSerializer
{
  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
  };

  public string Serialize<T>(T obj)
  {
    if (jsonSerializerContext is null)
      return JsonSerializer.Serialize(obj, Options);

    var typeInfo = jsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
        ?? throw new InvalidOperationException($"The JSON source-generation context does not contain metadata for '{typeof(T)}'.");

    return JsonSerializer.Serialize(obj, typeInfo);
  }

  public T? Deserialize<T>(string data)
  {
    if (jsonSerializerContext is null)
      return JsonSerializer.Deserialize<T>(data, Options);

    var typeInfo = jsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
        ?? throw new InvalidOperationException($"The JSON source-generation context does not contain metadata for '{typeof(T)}'.");

    return JsonSerializer.Deserialize(data, typeInfo);
  }

  public object? Deserialize(string data, Type type)
  {
    ArgumentNullException.ThrowIfNull(type);
    if (jsonSerializerContext is null)
      return JsonSerializer.Deserialize(data, type, Options);

    var typeInfo = jsonSerializerContext.GetTypeInfo(type)
        ?? throw new InvalidOperationException($"The JSON source-generation context does not contain metadata for '{type}'.");

    return JsonSerializer.Deserialize(data, typeInfo);
  }
}
