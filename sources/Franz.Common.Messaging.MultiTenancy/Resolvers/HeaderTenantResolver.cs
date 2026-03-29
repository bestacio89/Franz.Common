#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.MultiTenancy.Resolvers;

/// <summary>
/// Resolves the tenant from the "X-Tenant-ID" message header.
/// Senior Note: Updated to support the serializable string[] header contract for .NET 10.
/// </summary>
public sealed class HeaderTenantResolver(
    IMessageContextAccessor messageContextAccessor,
    ITenantStore tenantStore) : ITenantResolver
{
  private readonly IMessageContextAccessor _messageContextAccessor = messageContextAccessor;
  private readonly ITenantStore _tenantStore = tenantStore;

  public int Order => 0;

  public async Task<TenantResolutionResult?> ResolveTenantAsync(object? context = null)
  {
    var headers = _messageContextAccessor.Current?.Message.Headers;

    // SENIOR FIX: Manually resolve the TenantId from the string array to ensure 
    // alignment with our new IDictionary<string, string[]> contract.
    if (headers != null &&
        headers.TryGetValue(HeaderConstants.TenantId, out var values) &&
        values.Length > 0 &&
        Guid.TryParse(values[0], out var tenantId))
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

    // No header present or invalid format → skip this resolver
    return null;
  }
}