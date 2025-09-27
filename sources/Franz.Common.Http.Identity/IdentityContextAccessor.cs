using Franz.Common.Headers;
using Franz.Common.Http.Extensions;
using Franz.Common.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Franz.Common.Http.Identity;

public class IdentityContextAccessor : IIdentityContextAccessor
{
  private readonly IHttpContextAccessor httpContextAccessor;

  public IdentityContextAccessor(IHttpContextAccessor httpContextAccessor)
  {
    this.httpContextAccessor = httpContextAccessor;
  }

  public string? GetCurrentEmail()
  {
    return httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.UserEmail, ClaimTypes.Email);
  }

  public Guid? GetCurrentId()
  {
    var userId = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.UserId, ClaimTypes.NameIdentifier);
    return Guid.TryParse(userId, out var guid) ? guid : null;
  }

  public string? GetCurrentFullName()
  {
    return httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.UserFullName, ClaimTypes.Name);
  }

  public string[]? GetCurrentRoles()
  {
    var roles = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.UserRoles, ClaimTypes.Role);
    return string.IsNullOrWhiteSpace(roles)
        ? null
        : roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
  }

  public Guid? GetCurrentTenantId()
  {
    var tenantId = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.TenantId, "tenant_id");
    return Guid.TryParse(tenantId, out var guid) ? guid : null;
  }

  public Guid? GetCurrentDomainId()
  {
    var domainId = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.DomainId, "domain_id");
    return Guid.TryParse(domainId, out var guid) ? guid : null;
  }

  public FranzIdentityContext GetCurrentIdentity()
  {
    return new FranzIdentityContext
    {
      UserId = GetCurrentId(),
      Email = GetCurrentEmail(),
      FullName = GetCurrentFullName(),
      Roles = GetCurrentRoles(),
      TenantId = GetCurrentTenantId(),
      DomainId = GetCurrentDomainId()
    };
  }


}
