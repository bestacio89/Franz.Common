

namespace Franz.Common.MultiTenancy
{
  /// <summary>
  /// Orchestrates domain resolvers in order and returns the first valid DomainResolutionResult.
  /// </summary>
  public interface IDomainResolutionPipeline
  {
    /// <summary>
    /// Attempts to resolve the current domain using all registered resolvers.
    /// Returns the first successful DomainResolutionResult or a failed result if none succeed.
    /// </summary>
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    Task<DomainResolutionResult> ResolveAsync(object? context = null);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  }
}
