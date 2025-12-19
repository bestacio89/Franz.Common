using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization;

public sealed class SystemTextJsonSerializer : IJsonSerializer
{
  private readonly JsonSerializerOptions _options;

  public SystemTextJsonSerializer(IEnumerable<JsonConverter> converters)
  {
    _options = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      PropertyNameCaseInsensitive = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      WriteIndented = false
    };

    foreach (var converter in converters)
    {
      _options.Converters.Add(converter);
    }
  }

  public string? Serialize(object? content)
  {
    if (content is null)
      return null;

    return JsonSerializer.Serialize(content, content.GetType(), _options);
  }

  public TOut? Deserialize<TOut>(string? content)
  {
    if (string.IsNullOrWhiteSpace(content))
      return default;

    return JsonSerializer.Deserialize<TOut>(content, _options);
  }

  public object? Deserialize(string? content, Type targetType)
  {
    if (string.IsNullOrWhiteSpace(content))
      return null;

    return JsonSerializer.Deserialize(content, targetType, _options);
  }
}
