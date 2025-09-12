using Franz.Common.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Http.Client.Delegating;

public class DomainRequestBuilder : IRequestBuilder
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private readonly IDomainContextAccessor? domainContextAccessor;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public DomainRequestBuilder(IDomainContextAccessor? domainContextAccessor = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    this.domainContextAccessor = domainContextAccessor;
  }

  public bool CanBuild(HttpRequestMessage request)
  {
    var result = domainContextAccessor != null;

    return result;
  }

  public void Build(HttpRequestMessage request)
  {
    var id = domainContextAccessor?.GetCurrentDomainId();

    if (id != null)
      request.Headers.Add(HeaderConstants.DomainId, id.ToString());
  }
}
