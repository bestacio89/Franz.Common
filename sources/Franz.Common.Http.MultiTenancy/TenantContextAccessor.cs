using Franz.Common.Headers;
using Franz.Common.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy;

public class TenantContextAccessor : ITenantContextAccessor
{
  private readonly IHttpContextAccessor httpContextAccessor;

  public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
  {
    this.httpContextAccessor = httpContextAccessor;
  }

  public Guid? GetCurrentId()
  {
    var tenantId = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.TenantId);

    Guid? result = null;
    if (Guid.TryParse(tenantId, out var guid))
      result = guid;

    return result;
  }
}
