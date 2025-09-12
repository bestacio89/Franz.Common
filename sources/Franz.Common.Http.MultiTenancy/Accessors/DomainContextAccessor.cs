using Franz.Common.Headers;
using Franz.Common.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Accessors;

public class DomainContextAccessor : IDomainContextAccessor
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public DomainContextAccessor(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public Guid? GetCurrentDomainId()
  {
    if (_httpContextAccessor.HttpContext?.Request.Headers.ContainsKey(HeaderConstants.DomainId) == true)
    {
      var domainId = _httpContextAccessor.HttpContext.Request.Headers[HeaderConstants.DomainId];

      if (Guid.TryParse(domainId, out var guid))
        return guid;
    }

    return null;
  }

  public void SetCurrentDomainId(Guid domainId)
  {
    // Set the domain ID in the HTTP context headers
    if (_httpContextAccessor.HttpContext != null)
    {
      _httpContextAccessor.HttpContext.Request.Headers[HeaderConstants.DomainId] = domainId.ToString();
    }
  }
}
