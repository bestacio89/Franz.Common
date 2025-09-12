using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy.Pipeline;

public class DefaultDomainResolutionPipeline : IDomainResolutionPipeline
{
  private readonly IEnumerable<IDomainResolver> _resolvers;

  public DefaultDomainResolutionPipeline(IEnumerable<IDomainResolver> resolvers)
  {
    _resolvers = resolvers.OrderBy(r => r.Order);
  }

  public async Task<DomainResolutionResult> ResolveAsync(object? context = null)
  {
    foreach (var resolver in _resolvers)
    {
      var result = await resolver.ResolveDomainAsync(context);
      if (result.Success)
        return result;
    }

    return DomainResolutionResult.FailedResult("No resolver could determine domain.");
  }
}
