using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Resolvers;

public class QueryStringTenantResolver(ITenantStore store, string paramName = "tenantId") : ITenantResolver
{
    public int Order => 150;

    public Task<TenantResolutionResult?> ResolveTenantAsync(object? context = null
        => ResolveTenantAsync(context, CancellationToken.None);

    public async Task<TenantResolutionResult?> ResolveTenantAsync(
        object? context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context is not HttpContext httpContext ||
            !httpContext.Request.Query.TryGetValue(paramName, out var value))
            return null;

        if (!Guid.TryParse(value, out var tenantId))
            return TenantResolutionResult.FailedResult(
                TenantResolutionSource.QueryString,
                $"Query string parameter '{paramName}' was present but not a valid GUID.");

        var tenant = await store.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return tenant is null
            ? TenantResolutionResult.FailedResult(
                TenantResolutionSource.QueryString,
                $"Tenant '{tenantId}' not found for query string parameter '{paramName}'.")
            : TenantResolutionResult.Success(
                tenant,
                TenantResolutionSource.QueryString,
                $"Resolved tenant '{tenant.Name}' from query string parameter '{paramName}'.");
    }
}
