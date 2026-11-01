using System.Security.Claims;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.Http.MultiTenancy;

public interface ITenantRequestAuthorizer
{
    Task<bool> IsAuthorizedAsync(ClaimsPrincipal principal, TenantInfo tenant, CancellationToken cancellationToken = default);
}
