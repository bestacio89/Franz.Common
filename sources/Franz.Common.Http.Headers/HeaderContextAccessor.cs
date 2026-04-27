#nullable enable
using Franz.Common.Headers;
using Franz.Common.Serialization;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Http.Headers;

/// <summary>
/// Provides access to headers within the current HTTP context.
/// Senior Note: Bridges ASP.NET Core StringValues to our serializable string[] contract.
/// </summary>
public sealed class HeaderContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IJsonSerializer jsonSerializer) : IHeaderContextAccessor
{
  private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
  private readonly IJsonSerializer _jsonSerializer = jsonSerializer;

  public IDictionary<string, string[]> ListAll()
  {
    var headers = _httpContextAccessor.HttpContext?.Request?.Headers;

    if (headers is null)
      return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    return headers.ToDictionary(
        kvp => kvp.Key,
        kvp =>
        {
          var array = kvp.Value.ToArray();

          return array
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToArray();
        },
        StringComparer.OrdinalIgnoreCase
    );
  }

  public bool TryGetValue(string key, [NotNullWhen(true)] out string[]? value)
  {
    value = default;

    // FIX: Replaced .Current?.HttpContext with .HttpContext
    var headers = _httpContextAccessor.HttpContext?.Request?.Headers;

    if (headers != null && headers.TryGetValue(key, out var stringValues))
    {
      value = stringValues.ToArray()!;
      return true;
    }

    return false;
  }

  public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
  {
    value = default;

    if (!TryGetValue(key, out var values) || values.Length == 0)
      return false;

    var raw = values[0];
    if (string.IsNullOrWhiteSpace(raw))
      return false;

    if (typeof(T) == typeof(string))
    {
      value = (T)(object)raw;
      return true;
    }

    // Senior Note: Deserializing from the first header value found
    value = _jsonSerializer.Deserialize<T>(raw);
    return value is not null;
  }
}