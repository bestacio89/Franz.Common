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
public sealed class CachedTenantStore : ITenantStore
{
    internal const string AllTenantsCacheKey = "franz:tenants:store:all";
    private readonly ITenantStore _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _absoluteExpiration;
    private readonly TenantStoreCacheState _state;

    public CachedTenantStore(
        ITenantStore inner,
        IMemoryCache cache,
        IOptions<TenantStoreCacheOptions> options,
        TenantStoreCacheState state)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _state = state ?? throw new ArgumentNullException(nameof(state));
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
            AllTenantsCacheKey,
            async () =>
            {
                var snapshot = (await _inner.GetAllAsync().ConfigureAwait(false)).Select(Clone).ToArray();
                foreach (var tenant in snapshot)
                    TrackTenant(tenant);
                return snapshot;
            },
            static _ => { }).ConfigureAwait(false);

        return tenants.Select(Clone).ToArray();
    }

    public Task<bool> ExistsAsync(Guid id)
        => GetOrLoadValueAsync(ExistsKey(id), () => _inner.ExistsAsync(id));

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
            AllTenantsCacheKey,
            async () =>
            {
                var snapshot = (await _inner.GetAllAsync(cancellationToken).ConfigureAwait(false)).Select(Clone).ToArray();
                foreach (var tenant in snapshot)
                    TrackTenant(tenant);
                return snapshot;
            },
            static _ => { },
            cancellationToken).ConfigureAwait(false);

        return tenants.Select(Clone).ToArray();
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetOrLoadValueAsync(
            ExistsKey(id),
            () => _inner.ExistsAsync(id),
            cancellationToken);
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

        var gate = _state.PopulationGates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
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
            _state.TrackCacheKey(key);
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

        var gate = _state.PopulationGates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
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
            _state.TrackCacheKey(key);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    private void TrackTenant(TenantInfo tenant)
    {
        var hosts = _state.TenantHosts.GetOrAdd(
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

    internal static string TenantIdKey(Guid id) => $"franz:tenants:store:id:{id:D}";
    internal static string HostKey(string host) => $"franz:tenants:store:host:{host}";
    internal static string ExistsKey(Guid id) => $"franz:tenants:store:exists:{id:D}";
}

/// <summary>Shared cache bookkeeping so invalidation remains correct for any ITenantStore lifetime.</summary>
public sealed class TenantStoreCacheState
{
    internal ConcurrentDictionary<string, SemaphoreSlim> PopulationGates { get; } = new(StringComparer.Ordinal);
    internal ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> TenantHosts { get; } = new();
    internal ConcurrentDictionary<string, byte> KnownCacheKeys { get; } = new(StringComparer.Ordinal);

    internal void TrackCacheKey(string key) => KnownCacheKeys.TryAdd(key, 0);
}

/// <summary>Invalidates tenant-store entries across all decorated store instances in this process.</summary>
internal sealed class TenantStoreCacheInvalidator(
    IMemoryCache cache,
    TenantStoreCacheState state) : ITenantStoreCacheInvalidator
{
    public void InvalidateTenant(Guid tenantId)
    {
        Remove(CachedTenantStore.TenantIdKey(tenantId));
        Remove(CachedTenantStore.ExistsKey(tenantId));
        Remove(CachedTenantStore.AllTenantsCacheKey);

        if (state.TenantHosts.TryRemove(tenantId, out var hosts))
        {
            foreach (var host in hosts.Keys)
                Remove(CachedTenantStore.HostKey(host));
        }
    }

    public void InvalidateHost(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var normalizedHost = TenantDomain.NormalizeHost(host);
        Remove(CachedTenantStore.HostKey(normalizedHost));

        foreach (var tenantHosts in state.TenantHosts.Values)
            tenantHosts.TryRemove(normalizedHost, out _);
    }

    public void InvalidateAll()
    {
        foreach (var key in state.KnownCacheKeys.Keys)
            Remove(key);
        state.TenantHosts.Clear();
    }

    private void Remove(string key)
    {
        cache.Remove(key);
        state.KnownCacheKeys.TryRemove(key, out _);
    }
}
