// Resolvers/JwtClaimTenantResolver.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

#nullable enable
namespace Franz.Common.Http.MultiTenancy.Resolvers
{
  public class JwtClaimTenantResolver : ITenantResolver
  {
    private readonly ITenantStore _store;
    private readonly string _claimType;

    public JwtClaimTenantResolver(ITenantStore store, string claimType = "tenant_id")
    {
      _store = store;
      _claimType = claimType;
    }

    public int Order => 200;

    public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
    {
      if (context is not HttpContext httpContext)
        return null;

      var claim = httpContext.User.Claims.FirstOrDefault(c => c.Type == _claimType);
      if (claim == null)
        return null;

      if (Guid.TryParse(claim.Value, out var tenantId))
      {
        var tenant = await _store.FindByIdAsync(tenantId);
        if (tenant != null)
        {
          return TenantResolutionResult.Success(
            tenant,
            TenantResolutionSource.JwtClaim,
            $"Resolved tenant '{tenant.Name}' from JWT claim '{_claimType}'."
          );
        }

        return TenantResolutionResult.FailedResult(
          TenantResolutionSource.JwtClaim,
          $"Tenant '{tenantId}' not found for claim '{_claimType}'."
        );
      }

      return TenantResolutionResult.FailedResult(
        TenantResolutionSource.JwtClaim,
        $"Claim '{_claimType}' was present but not a valid GUID."
      );
    }
  }
}
