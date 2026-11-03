using System.Collections.Concurrent;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.MultiTenancy;

/// <summary>
/// Adds time-bounded, process-local caching to an existing tenant store. Registry writes remain
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
        => GetOrLoadAsync(TenantIdKey(id), () => _inner.FindByIdAsync(id), static _ => { });

    public Task<TenantInfo?> FindByHostAsync(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var normalizedHost = TenantDomain.NormalizeHost(host);
        return GetOrLoadAsync(HostKey(normalizedHost), () => _inner.FindByHostAsync(normalizedHost), tenant => TrackTenantHost(tenant, normalizedHost));
    }

    public async Task<IEnumerable<TenantInfo>> GetAllAsync()
    {
        var tenants = await GetOrLoadAsync(
            AllTenantsCacheKey,
            async () =>
            {
                var snapshot = (await _inner.GetAllAsync().ConfigureAwait(false)).Select(Clone).ToArray();
                return snapshot;
            },
            static _ => { }).ConfigureAwait(false);

        return tenants!.Select(Clone).ToArray();
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
                return snapshot;
            },
            static _ => { },
            cancellationToken).ConfigureAwait(false);

        return tenants!.Select(Clone).ToArray();
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

        await using var gate = await _state.AcquirePopulationGateAsync(key, cancellationToken).ConfigureAwait(false);
        if (_cache.TryGetValue(key, out cached))
            return cached is null ? null : CloneIfTenant(cached);

        var generation = _state.GetGeneration(key);
        var value = await factory().ConfigureAwait(false);
        if (value is null)
            return null;

        var snapshot = CloneIfTenant(value);
        if (StoreIfCurrent(key, snapshot, generation))
            onLoaded(value);
        return CloneIfTenant(snapshot);
    }

    private async Task<T> GetOrLoadValueAsync<T>(
        string key,
        Func<Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached!;

        await using var gate = await _state.AcquirePopulationGateAsync(key, cancellationToken).ConfigureAwait(false);
        if (_cache.TryGetValue(key, out cached))
            return cached!;

        var generation = _state.GetGeneration(key);
        var value = await factory().ConfigureAwait(false);
        StoreIfCurrent(key, value, generation);
        return value;
    }

    private bool StoreIfCurrent<T>(string key, T value, long generation)
    {
        if (_state.GetGeneration(key) != generation)
            return false;

        _cache.Set(key, value, CreateEntryOptions());
        _state.TrackCacheKey(key);
        if (_state.GetGeneration(key) == generation)
            return true;

        _cache.Remove(key);
        _state.KnownCacheKeys.TryRemove(key, out _);
        return false;
    }

    private MemoryCacheEntryOptions CreateEntryOptions()
        => new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _absoluteExpiration
        }.RegisterPostEvictionCallback(
            static (evictedKey, _, _, state) =>
            {
                var evictionState = (TenantStoreCacheEvictionState)state!;
                var key = (string)evictedKey!;
                if (!evictionState.Cache.TryGetValue(key, out _))
                {
                    evictionState.State.KnownCacheKeys.TryRemove(key, out _);
                    if (key.StartsWith("franz:tenants:store:host:", StringComparison.Ordinal))
                        evictionState.State.RemoveHostFromIndexes(key["franz:tenants:store:host:".Length..]);
                }
            },
            new TenantStoreCacheEvictionState(_state, _cache));

    private sealed record TenantStoreCacheEvictionState(TenantStoreCacheState State, IMemoryCache Cache);\n\n    private void TrackTenantHost(TenantInfo tenant, string host)
        => _state.TrackTenantHost(tenant.Id, host);

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
    private readonly ConcurrentDictionary<string, PopulationGate> _populationGates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _generations = new(StringComparer.Ordinal);

    internal ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> TenantHosts { get; } = new();
    internal ConcurrentDictionary<string, byte> KnownCacheKeys { get; } = new(StringComparer.Ordinal);

    internal void TrackTenantHost(Guid tenantId, string host)
    {
        var hosts = TenantHosts.GetOrAdd(
            tenantId,
            static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
        hosts.TryAdd(host, 0);
    }

    internal void RemoveHostFromIndexes(string host)
    {
        foreach (var hosts in TenantHosts.Values)
            hosts.TryRemove(host, out _);
    }

    internal long GetGeneration(string key) => _generations.GetOrAdd(key, 0);

    internal void TrackCacheKey(string key) => KnownCacheKeys.TryAdd(key, 0);

    internal string[] GetActiveKeys() => _populationGates.Keys.ToArray();

    internal void InvalidateKey(string key)
    {
        if (_populationGates.ContainsKey(key))
            _generations.AddOrUpdate(key, 1, static (_, generation) => checked(generation + 1));

        KnownCacheKeys.TryRemove(key, out _);
    }

    internal async Task<IAsyncDisposable> AcquirePopulationGateAsync(
        string key,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var entry = _populationGates.GetOrAdd(key, static _ => new PopulationGate());
            lock (entry.Sync)
            {
                if (entry.Removed ||
                    !_populationGates.TryGetValue(key, out var current) ||
                    !ReferenceEquals(current, entry))
                    continue;

                entry.ReferenceCount++;
            }

            try
            {
                await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                return new PopulationGateLease(this, key, entry);
            }
            catch
            {
                ReleaseReference(key, entry);
                throw;
            }
        }
    }

    private void Release(string key, PopulationGate entry)
    {
        entry.Semaphore.Release();
        ReleaseReference(key, entry);
    }

    private void ReleaseReference(string key, PopulationGate entry)
    {
        lock (entry.Sync)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount != 0)
                return;

            entry.Removed = true;
            _generations.TryRemove(key, out _);
            if (_populationGates.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
                _populationGates.TryRemove(key, out _);
            entry.Semaphore.Dispose();
        }
    }

    private sealed class PopulationGate
    {
        public object Sync { get; } = new();
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
        public bool Removed { get; set; }
    }

    private sealed class PopulationGateLease(
        TenantStoreCacheState owner,
        string key,
        PopulationGate entry) : IAsyncDisposable
    {
        private int _disposed;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.Release(key, entry);
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>Invalidates tenant-store entries across all decorated store instances in this process.</summary>
public sealed class TenantStoreCacheInvalidator(
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
        foreach (var key in state.GetActiveKeys().Concat(state.KnownCacheKeys.Keys).Distinct(StringComparer.Ordinal))
            Remove(key);
        state.TenantHosts.Clear();
    }

    private void Remove(string key)
    {
        state.InvalidateKey(key);
        cache.Remove(key);
    }
}
