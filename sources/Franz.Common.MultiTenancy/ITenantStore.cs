// File: ITenantStore.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy.Models;
#nullable enable
namespace Franz.Common.MultiTenancy
{
  /// <summary>
  /// Persistent storage/registry for tenants. I/O implementations should override
  /// the cancellation-aware overloads and pass tokens to their underlying provider.
  /// </summary>
  public interface ITenantStore
  {
    Task<TenantInfo?> FindByIdAsync(Guid id);
    Task<TenantInfo?> FindByHostAsync(string host);
    Task<IEnumerable<TenantInfo>> GetAllAsync();
    Task<bool> ExistsAsync(Guid id);

    Task<TenantInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return FindByIdAsync(id);
    }

    Task<TenantInfo?> FindByHostAsync(string host, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return FindByHostAsync(host);
    }

    Task<IEnumerable<TenantInfo>> GetAllAsync(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return GetAllAsync();
    }

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ExistsAsync(id);
    }
  }
}
