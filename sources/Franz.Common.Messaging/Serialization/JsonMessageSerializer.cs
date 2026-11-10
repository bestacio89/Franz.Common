using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Messaging.Serialization;

public class JsonMessageSerializer : IMessageSerializer
{
  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly JsonSerializerContext? _jsonSerializerContext;

  public JsonMessageSerializer()
  {
    EnsureReflectionSerializationIsSupported();
  }

  public JsonMessageSerializer(JsonSerializerContext jsonSerializerContext)
  {
    ArgumentNullException.ThrowIfNull(jsonSerializerContext);
    _jsonSerializerContext = jsonSerializerContext;
  }

  [UnconditionalSuppressMessage("Trimming", "IL2026",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  [UnconditionalSuppressMessage("AOT", "IL3050",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  public string Serialize<T>(T obj)
  {
    if (_jsonSerializerContext is null)
      return JsonSerializer.Serialize(obj, Options);

    var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
        ?? throw MissingMetadata(typeof(T));

    return JsonSerializer.Serialize(obj, typeInfo);
  }

  [UnconditionalSuppressMessage("Trimming", "IL2026",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  [UnconditionalSuppressMessage("AOT", "IL3050",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  public string Serialize(object obj, Type type)
  {
    ArgumentNullException.ThrowIfNull(obj);
    ArgumentNullException.ThrowIfNull(type);

    if (_jsonSerializerContext is null)
      return JsonSerializer.Serialize(obj, type, Options);

    var typeInfo = _jsonSerializerContext.GetTypeInfo(type)
        ?? throw MissingMetadata(type);

    return JsonSerializer.Serialize(obj, typeInfo);
  }

  [UnconditionalSuppressMessage("Trimming", "IL2026",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  [UnconditionalSuppressMessage("AOT", "IL3050",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  public T? Deserialize<T>(string data)
  {
    if (_jsonSerializerContext is null)
      return JsonSerializer.Deserialize<T>(data, Options);

    var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
        ?? throw MissingMetadata(typeof(T));

    return JsonSerializer.Deserialize(data, typeInfo);
  }

  [UnconditionalSuppressMessage("Trimming", "IL2026",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  [UnconditionalSuppressMessage("AOT", "IL3050",
      Justification = "The reflection fallback is guarded by the constructor's dynamic-code check. Native AOT hosts must provide source-generated metadata.")]
  public object? Deserialize(string data, Type type)
  {
    ArgumentNullException.ThrowIfNull(type);

    if (_jsonSerializerContext is null)
      return JsonSerializer.Deserialize(data, type, Options);

    var typeInfo = _jsonSerializerContext.GetTypeInfo(type)
        ?? throw MissingMetadata(type);

    return JsonSerializer.Deserialize(data, typeInfo);
  }

  private static InvalidOperationException MissingMetadata(Type type)
      => new($"The JSON source-generation context does not contain metadata for '{type}'.");

  private static void EnsureReflectionSerializationIsSupported()
  {
    if (!RuntimeFeature.IsDynamicCodeSupported)
      throw new InvalidOperationException(
          "Native AOT requires an application-provided JsonSerializerContext. " +
          "Register it with AddMessagingSerialization(JsonSerializerContext).");
  }
}
