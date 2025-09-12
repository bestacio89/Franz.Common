// Resolvers/HostTenantResolver.cs
using System.Threading.Tasks;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

#nullable enable
namespace Franz.Common.Http.MultiTenancy.Resolvers
{
  public class HostTenantResolver : ITenantResolver
  {
    private readonly ITenantStore _store;

    public HostTenantResolver(ITenantStore store)
    {
      _store = store;
    }

    public int Order => 50;

    public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
    {
      if (context is not HttpContext httpContext)
        return null; // not applicable for this resolver

      var host = httpContext.Request.Host.Host;
      if (string.IsNullOrWhiteSpace(host))
        return null; // not applicable (no host)

      var tenant = await _store.FindByHostAsync(host);
      if (tenant != null)
      {
        return TenantResolutionResult.Success(
          tenant,
          TenantResolutionSource.Host,
          $"Resolved tenant '{tenant.Name}' from host '{host}'."
        );
      }

      // We *attempted* with this source but couldn't resolve — return a failed result so the pipeline can log it and try next.
      return TenantResolutionResult.FailedResult(
        TenantResolutionSource.Host,
        $"No tenant matched host '{host}'."
      );
    }
  }
}
