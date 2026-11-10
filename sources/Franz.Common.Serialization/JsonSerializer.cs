using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Serialization;

public sealed class SystemTextJsonSerializer : IJsonSerializer
{
  private readonly JsonSerializerOptions _options;

  public SystemTextJsonSerializer(IEnumerable<JsonConverter> converters)
      : this(converters, CreateReflectionOptions())
  {
  }

  public SystemTextJsonSerializer(
      IEnumerable<JsonConverter> converters,
      JsonSerializerContext jsonSerializerContext)
      : this(converters, CreateSourceGeneratedOptions(jsonSerializerContext))
  {
  }

  private SystemTextJsonSerializer(
      IEnumerable<JsonConverter> converters,
      JsonSerializerOptions options)
  {
    ArgumentNullException.ThrowIfNull(converters);
    _options = options;

    foreach (var converter in converters)
      _options.Converters.Add(converter);
  }

  public string? Serialize(object? content)
  {
    if (content is null)
      return null;

    var typeInfo = _options.GetTypeInfo(content.GetType());
    return JsonSerializer.Serialize(content, typeInfo);
  }

  public TOut? Deserialize<TOut>(string? content)
  {
    if (string.IsNullOrWhiteSpace(content))
      return default;

    var typeInfo = _options.GetTypeInfo(typeof(TOut)) as JsonTypeInfo<TOut>
        ?? throw MissingMetadata(typeof(TOut));

    return JsonSerializer.Deserialize(content, typeInfo);
  }

  public object? Deserialize(string? content, Type targetType)
  {
    ArgumentNullException.ThrowIfNull(targetType);

    if (string.IsNullOrWhiteSpace(content))
      return null;

    var typeInfo = _options.GetTypeInfo(targetType);
    return JsonSerializer.Deserialize(content, typeInfo);
  }

  private static JsonSerializerOptions CreateReflectionOptions()
  {
    if (!RuntimeFeature.IsDynamicCodeSupported)
      throw new InvalidOperationException(
          "Native AOT requires an application-provided JsonSerializerContext. " +
          "Register it with AddSerializers(JsonSerializerContext).");

    return new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      PropertyNameCaseInsensitive = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      WriteIndented = false
    };
  }

  private static JsonSerializerOptions CreateSourceGeneratedOptions(
      JsonSerializerContext jsonSerializerContext)
  {
    ArgumentNullException.ThrowIfNull(jsonSerializerContext);

    return new JsonSerializerOptions(jsonSerializerContext.Options)
    {
      TypeInfoResolver = jsonSerializerContext,
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      PropertyNameCaseInsensitive = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      WriteIndented = false
    };
  }

  private static InvalidOperationException MissingMetadata(Type type)
      => new($"The configured JSON source-generation context does not contain metadata for '{type}'.");
}