using System.Text.Json;

namespace Franz.Common.Messaging.Serialization;

public class JsonMessageSerializer : IMessageSerializer
{
  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
  };

  public string Serialize<T>(T obj) =>
      JsonSerializer.Serialize(obj, Options);

  public T? Deserialize<T>(string data) =>
      JsonSerializer.Deserialize<T>(data, Options);

  public object? Deserialize(string data, Type type) =>
      JsonSerializer.Deserialize(data, type, Options);
}
