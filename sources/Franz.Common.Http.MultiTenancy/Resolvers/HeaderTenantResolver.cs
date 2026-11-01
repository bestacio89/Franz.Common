using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Resolvers;

public sealed class HeaderTenantResolver(ITenantStore store, string headerName = "X-Tenant-Id") : ITenantResolver
{
    public int Order => 100;

    public Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
        => ResolveTenantAsync(context, CancellationToken.None);

    public async Task<TenantResolutionResult?> ResolveTenantAsync(
        object? context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context is not HttpContext httpContext ||
            !httpContext.Request.Headers.TryGetValue(headerName, out var header))
            return null;

        if (!Guid.TryParse(header, out var tenantId))
            return TenantResolutionResult.FailedResult(
                TenantResolutionSource.Header,
                $"Header '{headerName}' was present but not a valid GUID.");

        var tenant = await store.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return tenant is null
            ? TenantResolutionResult.FailedResult(
                TenantResolutionSource.Header,
                $"Tenant '{tenantId}' not found for header '{headerName}'.")
            : TenantResolutionResult.Success(
                tenant,
                TenantResolutionSource.Header,
                $"Resolved tenant '{tenant.Name}' from header '{headerName}'.");
    }
}
