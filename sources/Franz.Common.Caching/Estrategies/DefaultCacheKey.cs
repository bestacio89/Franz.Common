using Franz.Common.Caching.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Franz.Common.Caching.Estrategies;

public sealed class DefaultCacheKeyStrategy : ICacheKeyStrategy
{
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
        var payload = JsonSerializer.Serialize(request, requestType);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var digest = SHA256.HashData(payloadBytes);

        return $"{typeName}:{Convert.ToHexString(digest)}";
    }
}
