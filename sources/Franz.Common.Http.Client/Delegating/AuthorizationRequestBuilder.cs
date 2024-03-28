using Franz.Common.Headers;

namespace Franz.Common.Http.Client.Delegating;

public class AuthorizationRequestBuilder : IRequestBuilder
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private readonly IHeaderContextAccessor? headerContextAccessor;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public AuthorizationRequestBuilder(IHeaderContextAccessor? headerContextAccessor = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    this.headerContextAccessor = headerContextAccessor;
  }

  public bool CanBuild(HttpRequestMessage request)
  {
    var result = headerContextAccessor != null;

    return result;
  }

  public void Build(HttpRequestMessage request)
  {
    if (headerContextAccessor!.TryGetValue(HeaderConstants.Authorization, out var result))
      request.Headers.Add(HeaderConstants.Authorization, result.ToString());
  }
}
