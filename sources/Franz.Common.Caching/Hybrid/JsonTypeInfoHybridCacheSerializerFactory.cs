using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.Hybrid;

internal sealed class JsonTypeInfoHybridCacheSerializerFactory : IHybridCacheSerializerFactory
{
  private readonly JsonSerializerOptions _serializerOptions;

  public JsonTypeInfoHybridCacheSerializerFactory(IOptions<JsonSerializerOptions> serializerOptions)
  {
    _serializerOptions = serializerOptions?.Value
        ?? throw new ArgumentNullException(nameof(serializerOptions));
  }

  public bool TryCreateSerializer<T>(
      [NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
  {
    if (typeof(T) == typeof(string) || typeof(T) == typeof(byte[]))
    {
      serializer = null;
      return false;
    }

    JsonTypeInfo<T> typeInfo;
    try
    {
      typeInfo = (JsonTypeInfo<T>)_serializerOptions.GetTypeInfo(typeof(T));
    }
    catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
    {
      throw new InvalidOperationException(
          $"No JSON metadata is registered for HybridCache value type '{typeof(T).FullName}'. " +
          "Register a source-generated JsonSerializerContext with AddFranzCachingJsonTypeInfoResolver.",
          exception);
    }

    serializer = new JsonTypeInfoHybridCacheSerializer<T>(typeInfo);
    return true;
  }

  private sealed class JsonTypeInfoHybridCacheSerializer<T>(JsonTypeInfo<T> typeInfo)
      : IHybridCacheSerializer<T>
  {
    public T Deserialize(ReadOnlySequence<byte> source)
    {
      return source.IsSingleSegment
          ? JsonSerializer.Deserialize(source.FirstSpan, typeInfo)!
          : JsonSerializer.Deserialize(source.ToArray(), typeInfo)!;
    }

    public void Serialize(T value, IBufferWriter<byte> target)
    {
      using var writer = new Utf8JsonWriter(target);
      JsonSerializer.Serialize(writer, value, typeInfo);
    }
  }
}
