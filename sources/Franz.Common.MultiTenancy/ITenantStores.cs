// File: Stores/InMemoryTenantStore.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy.Models;
#nullable enable
namespace Franz.Common.MultiTenancy.Stores
{
  /// <summary>
  /// Simple in-memory store — not for production. Helpful for unit tests and quick local demos.
  /// </summary>
  public class InMemoryTenantStore : ITenantStore
  {
    private readonly ConcurrentDictionary<Guid, TenantInfo> tenants = new();

    public InMemoryTenantStore(IEnumerable<TenantInfo>? initial = null)
    {
      if (initial != null)
      {
        foreach (var t in initial)
          tenants[t.Id] = t;
      }
    }

    public Task<TenantInfo?> FindByIdAsync(Guid id)
    {
      tenants.TryGetValue(id, out var t);
      return Task.FromResult<TenantInfo?>(t);
    }

    public Task<TenantInfo?> FindByHostAsync(string host)
    {
      if (string.IsNullOrWhiteSpace(host)) return Task.FromResult<TenantInfo?>(null);
      var normalized = host.Trim().ToLowerInvariant();

      var tenant = tenants.Values.FirstOrDefault(t => t.Domains.Any(d => d.Host == normalized));
      return Task.FromResult<TenantInfo?>(tenant);
    }

    public Task<IEnumerable<TenantInfo>> GetAllAsync()
        => Task.FromResult(tenants.Values.AsEnumerable());

    public Task<bool> ExistsAsync(Guid id)
        => Task.FromResult(tenants.ContainsKey(id));

    // helper for tests / bootstrapping
    public void AddOrUpdate(TenantInfo tenant) => tenants[tenant.Id] = tenant;
  }
}
