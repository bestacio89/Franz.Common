using System.Threading.Tasks;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;

namespace Franz.Common.Messaging.MultiTenancy.Resolvers;

public class HeaderTenantResolver : ITenantResolver
{
  private readonly IMessageContextAccessor _messageContextAccessor;
  private readonly ITenantStore _tenantStore;

  public HeaderTenantResolver(IMessageContextAccessor messageContextAccessor, ITenantStore tenantStore)
  {
    _messageContextAccessor = messageContextAccessor;
    _tenantStore = tenantStore;
  }

  public int Order => 0;

  public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context = null)
  {
    if (_messageContextAccessor.Current?.Message.Headers.TryGetTenantId(out var tenantId) == true)
    {
      var tenant = await _tenantStore.FindByIdAsync(tenantId);
      if (tenant != null)
      {
        return TenantResolutionResult.Success(
          tenant,
          TenantResolutionSource.Header,
          $"Resolved tenant '{tenant.Name}' from message header."
        );
      }

      return TenantResolutionResult.FailedResult(
        TenantResolutionSource.Header,
        $"Tenant '{tenantId}' not found for message header."
      );
    }

    // No header present at all → not applicable for this resolver
    return null;
  }
}
