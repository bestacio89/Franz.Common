using System;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Resolvers;

public class QueryStringTenantResolver : ITenantResolver
{
  private readonly ITenantStore _store;
  private readonly string _paramName;

  public QueryStringTenantResolver(ITenantStore store, string paramName = "tenantId")
  {
    _store = store;
    _paramName = paramName;
  }

  public int Order => 150;

  public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context = null)
  {
    if (context is not HttpContext httpContext)
      return null;

    if (!httpContext.Request.Query.TryGetValue(_paramName, out var value))
      return null; // not applicable, query parameter missing

    if (Guid.TryParse(value, out var tenantId))
    {
      var tenant = await _store.FindByIdAsync(tenantId);
      if (tenant != null)
      {
        return TenantResolutionResult.Success(
          tenant,
          TenantResolutionSource.QueryString,
          $"Resolved tenant '{tenant.Name}' from query string parameter '{_paramName}'."
        );
      }

      return TenantResolutionResult.FailedResult(
        TenantResolutionSource.QueryString,
        $"Tenant '{tenantId}' not found for query string parameter '{_paramName}'."
      );
    }

    return TenantResolutionResult.FailedResult(
      TenantResolutionSource.QueryString,
      $"Query string parameter '{_paramName}' was present but not a valid GUID."
    );
  }
}
