using System;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Franz.Common.Messaging.Contexting;

namespace Franz.Common.Messaging.MultiTenancy.Resolvers;

public class MessagePropertyTenantResolver : ITenantResolver
{
  private readonly IMessageContextAccessor _messageContextAccessor;
  private readonly ITenantStore _tenantStore;

  public MessagePropertyTenantResolver(IMessageContextAccessor messageContextAccessor, ITenantStore tenantStore)
  {
    _messageContextAccessor = messageContextAccessor;
    _tenantStore = tenantStore;
  }

  public int Order => 1;

  public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context = null)
  {
    var msg = _messageContextAccessor.Current?.Message;
    if (msg == null)
      return null;

    if (!msg.TryGetProperty("tenant_id", out string tenantIdStr))
      return null; // not applicable → property not present

    if (Guid.TryParse(tenantIdStr, out var tenantId))
    {
      var tenant = await _tenantStore.FindByIdAsync(tenantId);
      if (tenant != null)
      {
        return TenantResolutionResult.Success(
          tenant,
          TenantResolutionSource.Property,
          $"Resolved tenant '{tenant.Name}' from message property 'tenant_id'."
        );
      }

      return TenantResolutionResult.FailedResult(
        TenantResolutionSource.Property,
        $"Tenant '{tenantId}' not found for message property 'tenant_id'."
      );
    }

    return TenantResolutionResult.FailedResult(
      TenantResolutionSource.Property,
      $"Message property 'tenant_id' was present but not a valid GUID."
    );
  }
}
