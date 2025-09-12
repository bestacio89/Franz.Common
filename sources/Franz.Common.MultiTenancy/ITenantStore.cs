// File: ITenantStore.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy.Models;
#nullable enable
namespace Franz.Common.MultiTenancy
{
  /// <summary>
  /// Persistent storage/registry for tenants. Keep async for IO implementations.
  /// </summary>
  public interface ITenantStore
  {
    Task<TenantInfo?> FindByIdAsync(Guid id);
    Task<TenantInfo?> FindByHostAsync(string host); // host is normalized e.g. "tenant.example.com"
    Task<IEnumerable<TenantInfo>> GetAllAsync();
    Task<bool> ExistsAsync(Guid id);
  }
}
