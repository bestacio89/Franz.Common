using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Caching.Estrategies;

/// <summary>
/// Builds opaque, deterministic cache keys using configured JSON metadata.
/// Native AOT applications should register source-generated metadata for every
/// cached request type.
/// </summary>
public class DefaultCacheKeyStrategy : ICacheKeyStrategy
{
    private readonly JsonSerializerOptions _serializerOptions;

    public DefaultCacheKeyStrategy()
        : this(new JsonSerializerOptions(JsonSerializerDefaults.General))
    {
    }

    public DefaultCacheKeyStrategy(IOptions<JsonSerializerOptions> serializerOptions)
        : this(serializerOptions?.Value ?? throw new ArgumentNullException(nameof(serializerOptions)))
    {
    }

    public DefaultCacheKeyStrategy(JsonSerializerOptions serializerOptions)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
    }

    public string GetKey(object request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return BuildKey(request);
    }

    public string BuildKey<TRequest>(TRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var typeName = requestType.FullName ?? requestType.Name;
        JsonTypeInfo typeInfo;

        try
        {
            typeInfo = _serializerOptions.GetTypeInfo(requestType);
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"No JSON metadata is registered for cache request type '{requestType.FullName}'. " +
                "Register a source-generated JsonSerializerContext with AddFranzCachingJsonTypeInfoResolver.",
                exception);
        }

        var payload = JsonSerializer.Serialize(request, typeInfo);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return $"{typeName}:{Convert.ToHexString(digest)}";
    }
}
