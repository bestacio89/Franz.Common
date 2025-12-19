using Franz.Common.Headers;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;

namespace Franz.Common.Http.Client.Delegating;

public sealed class AuthorizationRequestBuilder : IRequestBuilder
{
  private readonly IHeaderContextAccessor _headerContextAccessor;

  public AuthorizationRequestBuilder(IHeaderContextAccessor headerContextAccessor)
  {
    _headerContextAccessor = headerContextAccessor;
  }

  public bool CanBuild(HttpRequestMessage request)
  {
    if (request.Headers.Authorization is not null)
      return false;

    return _headerContextAccessor.TryGetValue(
             HeaderConstants.Authorization,
             out StringValues values)
           && values.Count > 0
           && !StringValues.IsNullOrEmpty(values);
  }

  public void Build(HttpRequestMessage request)
  {
    if (request.Headers.Authorization is not null)
      return;

    if (!_headerContextAccessor.TryGetValue(
          HeaderConstants.Authorization,
          out StringValues values))
      return;

    var token = values.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(token))
      return;

    request.Headers.Authorization =
        AuthenticationHeaderValue.Parse(token);
  }
}
