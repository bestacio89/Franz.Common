// File: ITenantResolutionPipeline.cs
using Franz.Common.MultiTenancy.TenantResolution;
using System.Threading;
using System.Threading.Tasks;
#nullable enable
namespace Franz.Common.MultiTenancy
{
  /// <summary>Orchestrates resolvers and returns the tenant resolution result.</summary>
  public interface ITenantResolutionPipeline
  {
    Task<TenantResolutionResult?> ResolveAsync(object? context);

    Task<TenantResolutionResult?> ResolveAsync(object? context, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ResolveAsync(context);
    }
  }
}
