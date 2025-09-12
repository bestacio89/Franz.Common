using Franz.Common.Headers;
using Franz.Common.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Accessors;

public class TenantContextAccessor : ITenantContextAccessor
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public Guid? GetCurrentTenantId()
  {
    if (_httpContextAccessor.HttpContext?.Request.Headers.ContainsKey(HeaderConstants.TenantId) == true)
    {
      var tenantId = _httpContextAccessor.HttpContext.Request.Headers[HeaderConstants.TenantId];

      if (Guid.TryParse(tenantId, out var guid))
        return guid;
    }

    return null;
  }

  public void SetCurrentTenantId(Guid tenantId)
  {
    if (_httpContextAccessor.HttpContext != null)
    {
      _httpContextAccessor.HttpContext.Request.Headers[HeaderConstants.TenantId] = tenantId.ToString();
    }
  }
}
