using System.Security.Claims;
using Franz.Common.Http.MultiTenancy.Configuration;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.Options;

namespace Franz.Common.Http.MultiTenancy;

/// <summary>
/// Authorizes a principal only when it carries a tenant identifier claim whose
/// GUID exactly matches the already-resolved tenant.
/// </summary>
public sealed class ClaimsTenantRequestAuthorizer(
    IOptions<TenantClaimsAuthorizationOptions> options) : ITenantRequestAuthorizer
{
    public Task<bool> IsAuthorizedAsync(
        ClaimsPrincipal principal,
        TenantInfo tenant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();

        var claimType = options.Value.TenantIdClaimType;
        var authorized = principal.Identity?.IsAuthenticated == true &&
            principal.FindAll(claimType).Any(claim =>
                Guid.TryParse(claim.Value, out var claimedTenantId) &&
                claimedTenantId == tenant.Id);

        return Task.FromResult(authorized);
    }
}
