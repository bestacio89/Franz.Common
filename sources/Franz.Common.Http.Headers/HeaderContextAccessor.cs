using Franz.Common.Headers;
using Franz.Common.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Http.Headers;

public class HeaderContextAccessor : IHeaderContextAccessor
{
  private readonly IHttpContextAccessor httpContextAccessor;
  private readonly IJsonSerializer jsonSerializer;

  public HeaderContextAccessor(IHttpContextAccessor httpContextAccessor, IJsonSerializer jsonSerializer)
  {
    this.httpContextAccessor = httpContextAccessor;
    this.jsonSerializer = jsonSerializer;
  }

  public IEnumerable<KeyValuePair<string, StringValues>> ListAll()
  {
    IEnumerable<KeyValuePair<string, StringValues>> result = new List<KeyValuePair<string, StringValues>>();
    var headers = httpContextAccessor.HttpContext?.Request?.Headers;

    if (headers != null)
      result = headers;

    return result;
  }

  public bool TryGetValue(string key, out StringValues value)
  {
    value = default;

    var result = httpContextAccessor.HttpContext?.Request?.Headers.TryGetValue(key, out value);

    return result.HasValue && result.Value;
  }

  public bool TryGetValue<T>(string key, [MaybeNull] out T value)
  {
    StringValues stringValue = default;

    var result = httpContextAccessor.HttpContext?.Request?.Headers.TryGetValue(key, out stringValue) == true;

    value = jsonSerializer.Deserialize<T>(stringValue);

    return result;
  }
}
