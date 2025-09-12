using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;

namespace Franz.Common.Messaging.MultiTenancy.Pipeline;

public class DefaultTenantResolutionPipeline : ITenantResolutionPipeline
{
  private readonly IEnumerable<ITenantResolver> _resolvers;

  public DefaultTenantResolutionPipeline(IEnumerable<ITenantResolver> resolvers)
  {
    _resolvers = resolvers.OrderBy(r => r.Order);
  }

  public async Task<TenantResolutionResult?> ResolveAsync(object? context = null)
  {
    foreach (var resolver in _resolvers)
    {
      var result = await resolver.ResolveTenantAsync(context);
      if (result?.Tenant != null)
        return result;
    }

    return null;
  }
}
