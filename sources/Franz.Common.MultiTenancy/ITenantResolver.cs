// File: ITenantResolver.cs
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
    /// <summary>
    /// Try to resolve a tenant identifier from the provided ambient context object.
    /// The method accepts an opaque state object to avoid pulling ASP.NET types into the core library.
    /// The HTTP adapter will pass in a context wrapper that exposes Host/Header/Query/Cookie as needed.
    /// Return null if resolution failed.
    /// </summary>
    Task<TenantResolutionResult?> ResolveTenantAsync(object? context);

    /// <summary>
    /// Order of execution: lower values execute earlier.
    /// </summary>
    int Order { get; }
  }
}
