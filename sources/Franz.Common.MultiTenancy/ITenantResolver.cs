// File: ITenantResolver.cs
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy.TenantResolution;
#nullable enable

namespace Franz.Common.MultiTenancy
{
  /// <summary>
  /// Framework-agnostic contract for a strategy that attempts to resolve a tenant.
  /// Implementations should be lightweight and safe to execute for every request.
  /// </summary>
  public interface ITenantResolver
  {
    /// <summary>Attempts to resolve a tenant from the supplied ambient context.</summary>
    Task<TenantResolutionResult?> ResolveTenantAsync(object? context);

    /// <summary>
    /// Cancellation-aware resolution. Existing implementations remain compatible;
    /// I/O-backed resolvers should override this overload and propagate the token.
    /// </summary>
    Task<TenantResolutionResult?> ResolveTenantAsync(object? context, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ResolveTenantAsync(context);
    }

    /// <summary>Order of execution: lower values execute earlier.</summary>
    int Order { get; }
  }
}
