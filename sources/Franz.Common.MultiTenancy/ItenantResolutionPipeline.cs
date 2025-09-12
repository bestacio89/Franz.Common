// File: ITenantResolutionPipeline.cs
using Franz.Common.MultiTenancy.TenantResolution;
using System.Threading.Tasks;
#nullable enable
namespace Franz.Common.MultiTenancy
{
  /// <summary>
  /// Orchestrates resolvers in order and returns the first valid TenantResolutionResult.
  /// </summary>
  public interface ITenantResolutionPipeline
  {
    Task<TenantResolutionResult?> ResolveAsync(object? context);
  }
}
