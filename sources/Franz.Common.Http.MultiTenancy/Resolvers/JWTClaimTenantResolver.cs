using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Resolvers;

public sealed class JwtClaimTenantResolver(ITenantStore store, string claimType = "tenant_id") : ITenantResolver
{
    public int Order => 200;

    public Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
        => ResolveTenantAsync(context, CancellationToken.None);

    public async Task<TenantResolutionResult?> ResolveTenantAsync(
        object? context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context is not HttpContext httpContext || httpContext.User.Identity?.IsAuthenticated != true)
            return null;

        var claim = httpContext.User.FindFirst(claimType);
        if (claim is null)
            return null;
        if (!Guid.TryParse(claim.Value, out var tenantId))
            return TenantResolutionResult.FailedResult(
                TenantResolutionSource.JwtClaim,
                $"Authenticated claim '{claimType}' is not a valid tenant identifier.");

        var tenant = await store.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return tenant is null
            ? TenantResolutionResult.FailedResult(TenantResolutionSource.JwtClaim, "The authenticated tenant does not exist.")
            : TenantResolutionResult.Success(tenant, TenantResolutionSource.JwtClaim);
    }
}
