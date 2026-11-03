using System.Collections.Concurrent;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.MultiTenancy;

/// <summary>
/// Adds bounded, process-local caching to an existing tenant store. Registry writes remain
/// owned by the underlying store; callers must use the invalidator after successful writes.
/// </summary>
public sealed class CachedTenantStore : ITenantStore, ITenantStoreCacheInvalidator
{
    private const string AllTenantsKey = "franz:tenants:store:all";
    private readonly ITenantStore _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _absoluteExpiration;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _populationGates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _tenantHosts = new();

    public CachedTenantStore(
        ITenantStore inner,
        IMemoryCache cache,
        IOptions<TenantStoreCacheOptions> options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(options);

        _absoluteExpiration = options.Value.AbsoluteExpiration;
        if (_absoluteExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "Tenant-store cache expiration must be greater than zero.");
    }

    public Task<TenantInfo?> FindByIdAsync(Guid id)
        => GetOrLoadAsync(TenantIdKey(id), () => _inner.FindByIdAsync(id), TrackTenant);

    public Task<TenantInfo?> FindByHostAsync(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var normalizedHost = TenantDomain.NormalizeHost(host);
        return GetOrLoadAsync(HostKey(normalizedHost), () => _inner.FindByHostAsync(normalizedHost), TrackTenant);
    }

    public async Task<IEnumerable<TenantInfo>> GetAllAsync()
    {
        var tenants = await GetOrLoadAsync(
            AllTenantsKey,
            async () => (await _inner.GetAllAsync().ConfigureAwait(false)).Select(Clone).ToArray(),
            static _ => { }).ConfigureAwait(false);

        return tenants.Select(Clone).ToArray();
    }

    public Task<bool> ExistsAsync(Guid id)
        => GetOrLoadValueAsync($"franz:tenants:store:exists:{id:D}", () => _inner.ExistsAsync(id));

    public async Task<TenantInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetOrLoadAsync(
            TenantIdKey(id),
            () => _inner.FindByIdAsync(id, cancellationToken),
            TrackTenant,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantInfo?> FindByHostAsync(string host, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var normalizedHost = TenantDomain.NormalizeHost(host);
        return await GetOrLoadAsync(
            HostKey(normalizedHost),
            () => _inner.FindByHostAsync(normalizedHost, cancellationToken),
            TrackTenant,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TenantInfo>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tenants = await GetOrLoadAsync(
            AllTenantsKey,
            async () => (await _inner.GetAllAsync(cancellationToken).ConfigureAwait(false)).Select(Clone).ToArray(),
            static _ => { },
            cancellationToken).ConfigureAwait(false);

        return tenants.Select(Clone).ToArray();
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetOrLoadValueAsync(
            $"franz:tenants:store:exists:{id:D}",
            () => _inner.ExistsAsync(id),
            cancellationToken);
    }

    public void InvalidateTenant(Guid tenantId)
    {
        _cache.Remove(TenantIdKey(tenantId));
        _cache.Remove($"franz:tenants:store:exists:{tenantId:D}");
        _cache.Remove(AllTenantsKey);

        if (_tenantHosts.TryRemove(tenantId, out var hosts))
        {
            foreach (var host in hosts.Keys)
                _cache.Remove(HostKey(host));
        }
    }

    public void InvalidateHost(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var normalizedHost = TenantDomain.NormalizeHost(host);
        _cache.Remove(HostKey(normalizedHost));

        foreach (var tenantHosts in _tenantHosts.Values)
            tenantHosts.TryRemove(normalizedHost, out _);
    }

    public void InvalidateAll()
    {
        _cache.Remove(AllTenantsKey);
        // Entries are tracked by tenant/host indexes so invalidation is deterministic.
        foreach (var tenantId in _tenantHosts.Keys.ToArray())
            InvalidateTenant(tenantId);
        // Existence keys are not part of the host index; retain the keys we have observed.
        foreach (var key in _populationGates.Keys)
            _cache.Remove(key);
    }

    private async Task<T?> GetOrLoadAsync<T>(
        string key,
        Func<Task<T?>> factory,
        Action<T> onLoaded,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached is null ? null : CloneIfTenant(cached);

        var gate = _populationGates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(key, out cached))
                return cached is null ? null : CloneIfTenant(cached);

            var value = await factory().ConfigureAwait(false);
            if (value is null)
                return null;

            onLoaded(value);
            var snapshot = CloneIfTenant(value);
            _cache.Set(key, snapshot, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _absoluteExpiration
            });
            return CloneIfTenant(snapshot);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<T> GetOrLoadValueAsync<T>(
        string key,
        Func<Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached!;

        var gate = _populationGates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(key, out cached))
                return cached!;

            var value = await factory().ConfigureAwait(false);
            _cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _absoluteExpiration
            });
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    private void TrackTenant(TenantInfo tenant)
    {
        var hosts = _tenantHosts.GetOrAdd(
            tenant.Id,
            static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
        foreach (var domain in tenant.Domains)
            hosts.TryAdd(domain.Host, 0);
    }

    private static T CloneIfTenant<T>(T value)
        => value is TenantInfo tenant ? (T)(object)Clone(tenant) : value;

    private static TenantInfo Clone(TenantInfo tenant)
    {
        var copy = new TenantInfo(tenant.Id, tenant.Name)
        {
            Status = tenant.Status,
            ConnectionStringKey = tenant.ConnectionStringKey
        };

        foreach (var domain in tenant.Domains)
            copy.Domains.Add(new TenantDomain(domain.Host, domain.IsPrimary)
            {
                Id = domain.Id,
                TenantId = domain.TenantId,
                IsVerified = domain.IsVerified
            });

        foreach (var property in tenant.Properties)
            copy.Properties[property.Key] = property.Value;

        return copy;
    }

    private static string TenantIdKey(Guid id) => $"franz:tenants:store:id:{id:D}";
    private static string HostKey(string host) => $"franz:tenants:store:host:{host}";
}
