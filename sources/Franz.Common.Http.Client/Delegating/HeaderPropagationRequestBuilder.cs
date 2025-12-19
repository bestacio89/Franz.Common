using Franz.Common.Extensions;
using Franz.Common.Headers;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Http.Client.Delegating;

public sealed class HeaderPropagationRequestBuilder : IRequestBuilder
{
  private readonly IHeaderContextAccessor _headerContextAccessor;
  private readonly IReadOnlyCollection<string> _headersToPropagate;

  public HeaderPropagationRequestBuilder(
  IHeaderContextAccessor headerContextAccessor,
  IHeaderPropagationRegistrer? headerPropagationRegistrer = null,
  HeaderPropagationOptions? headerPropagationOptions = null)
  {
    _headerContextAccessor = headerContextAccessor;

    _headersToPropagate =
      (headerPropagationOptions?.Headers ?? Enumerable.Empty<string>())
        .Concat(
          headerPropagationRegistrer?.Headers.Select(h => h.HeaderName)
          ?? Enumerable.Empty<string>()
        )
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
  }

  public bool CanBuild(HttpRequestMessage request)
    => _headersToPropagate.Count > 0;

  public void Build(HttpRequestMessage request)
  {
    foreach (var headerName in _headersToPropagate)
    {
      if (!_headerContextAccessor.TryGetValue(headerName, out StringValues values))
        continue;

      if (StringValues.IsNullOrEmpty(values))
        continue;

      // HttpClient handles multi-values correctly
      request.Headers.Remove(headerName);
      request.Headers.Add(headerName, values.ToArray());
    }
  }
}
