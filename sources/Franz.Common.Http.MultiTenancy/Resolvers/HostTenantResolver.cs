using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Resolvers;

/// <summary>Resolves a tenant only through a verified canonical hostname mapping.</summary>
public sealed class HostTenantResolver(ITenantStore store) : ITenantResolver
{
    public int Order => 50;

    public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
    {
        if (context is not HttpContext httpContext)
            return null;
        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
            return null;

        string normalizedHost;
        try { normalizedHost = TenantDomain.NormalizeHost(host); }
        catch (ArgumentException)
        {
            return TenantResolutionResult.FailedResult(TenantResolutionSource.Host, "The request host is invalid.");
        }

        var tenant = await store.FindByHostAsync(normalizedHost).ConfigureAwait(false);
        if (tenant is null)
            return TenantResolutionResult.FailedResult(TenantResolutionSource.Host, "No tenant matched the request host.");

        var verifiedMapping = tenant.Domains.Any(domain =>
            domain.IsVerified && domain.Host == normalizedHost &&
            (domain.TenantId == Guid.Empty || domain.TenantId == tenant.Id));

        return verifiedMapping
            ? TenantResolutionResult.Success(tenant, TenantResolutionSource.Host)
            : TenantResolutionResult.FailedResult(TenantResolutionSource.Host, "The tenant hostname is not verified or does not belong to the resolved tenant.");
    }
}
