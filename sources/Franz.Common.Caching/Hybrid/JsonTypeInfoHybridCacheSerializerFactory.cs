using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.Hybrid;

/// <summary>
/// Uses the application's configured System.Text.Json metadata for HybridCache.
/// This allows the same source-generated context to serve cache keys, JSON
/// distributed providers, and HybridCache serialization.
/// </summary>
internal sealed class JsonTypeInfoHybridCacheSerializerFactory(
    IOptions<JsonSerializerOptions> serializerOptions) : IHybridCacheSerializerFactory
{
  private readonly JsonSerializerOptions _serializerOptions =
      serializerOptions?.Value ?? throw new ArgumentNullException(nameof(serializerOptions));

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
        => JsonSerializer.Deserialize(source, typeInfo)!;

    public void Serialize(T value, IBufferWriter<byte> target)
        => JsonSerializer.Serialize(target, value, typeInfo);
  }
}
