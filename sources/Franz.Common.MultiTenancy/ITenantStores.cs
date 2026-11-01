using System.Collections.Concurrent;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.MultiTenancy.Stores;

/// <summary>In-memory tenant registry for tests and local demonstrations; not a production store.</summary>
public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly ConcurrentDictionary<Guid, TenantInfo> _tenants = new();

    public InMemoryTenantStore(IEnumerable<TenantInfo>? initial = null)
    {
        if (initial is null) return;
        foreach (var tenant in initial) AddOrUpdate(tenant);
    }

    public Task<TenantInfo?> FindByIdAsync(Guid id)
    {
        _tenants.TryGetValue(id, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<TenantInfo?> FindByHostAsync(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return Task.FromResult<TenantInfo?>(null);
        string normalized;
        try { normalized = TenantDomain.NormalizeHost(host); }
        catch (ArgumentException) { return Task.FromResult<TenantInfo?>(null); }

        var matches = _tenants.Values
            .Where(tenant => tenant.Domains.Any(domain => domain.IsVerified && domain.Host == normalized))
            .Take(2)
            .ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException($"Hostname '{normalized}' is mapped to more than one tenant.");
        return Task.FromResult(matches.SingleOrDefault());
    }

    public Task<IEnumerable<TenantInfo>> GetAllAsync()
        => Task.FromResult<IEnumerable<TenantInfo>>(_tenants.Values.ToArray());

    public Task<bool> ExistsAsync(Guid id)
        => Task.FromResult(_tenants.ContainsKey(id));

    public void AddOrUpdate(TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        foreach (var domain in tenant.Domains)
        {
            var duplicate = _tenants.Values.Any(existing =>
                existing.Id != tenant.Id && existing.Domains.Any(candidate =>
                    candidate.IsVerified && domain.IsVerified && candidate.Host == domain.Host));
            if (duplicate)
                throw new InvalidOperationException($"Hostname '{domain.Host}' is already assigned to another tenant.");
        }
        _tenants[tenant.Id] = tenant;
    }
}
