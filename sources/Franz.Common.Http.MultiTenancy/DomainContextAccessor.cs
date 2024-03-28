using Franz.Common.Headers;
using Franz.Common.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy;

public class DomainContextAccessor : IDomainContextAccessor
{
  private readonly IHttpContextAccessor httpContextAccessor;

  public DomainContextAccessor(IHttpContextAccessor httpContextAccessor)
  {
    this.httpContextAccessor = httpContextAccessor;
  }

  public Guid? GetCurrentId()
  {
    Guid? result = null;

    if (httpContextAccessor.HttpContext?.Request.Headers.ContainsKey(HeaderConstants.DomainId) == true)
    {
      var domainId = httpContextAccessor.HttpContext?.Request.Headers[HeaderConstants.DomainId];

      if (Guid.TryParse(domainId, out var guid))
        result = guid;
    }

    return result;
  }
}
