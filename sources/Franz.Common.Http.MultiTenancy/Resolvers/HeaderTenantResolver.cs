// Resolvers/HeaderTenantResolver.cs
using System;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

#nullable enable
namespace Franz.Common.Http.MultiTenancy.Resolvers
{
  public class HeaderTenantResolver : ITenantResolver
  {
    private readonly ITenantStore _store;
    private readonly string _headerName;

    public HeaderTenantResolver(ITenantStore store, string headerName = "X-Tenant-Id")
    {
      _store = store;
      _headerName = headerName;
    }

    public int Order => 100;

    public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
    {
      if (context is not HttpContext httpContext)
        return null;

      if (!httpContext.Request.Headers.TryGetValue(_headerName, out var header))
        return null; // not applicable, header missing

      if (Guid.TryParse(header, out var tenantId))
      {
        var tenant = await _store.FindByIdAsync(tenantId);
        if (tenant != null)
        {
          return TenantResolutionResult.Success(
            tenant,
            TenantResolutionSource.Header,
            $"Resolved tenant '{tenant.Name}' from header '{_headerName}'."
          );
        }

        return TenantResolutionResult.FailedResult(
          TenantResolutionSource.Header,
          $"Tenant '{tenantId}' not found for header '{_headerName}'."
        );
      }

      return TenantResolutionResult.FailedResult(
        TenantResolutionSource.Header,
        $"Header '{_headerName}' was present but not a valid GUID."
      );
    }
  }
}
