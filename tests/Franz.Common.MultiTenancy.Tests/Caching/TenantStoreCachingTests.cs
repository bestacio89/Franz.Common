using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.MultiTenancy.Tests.Caching;

public sealed class TenantStoreCachingTests
{
    [Fact]
    public async Task FindByIdAsync_CachesSuccessfulLookupUntilInvalidated()
    {
        var store = new MutableTenantStore();
        var original = new TenantInfo(Guid.NewGuid(), "before");
        store.Set(original);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();

        Assert.Equal("before", (await cached.FindByIdAsync(original.Id))!.Name);

        store.Set(new TenantInfo(original.Id, "after"));

        Assert.Equal("before", (await cached.FindByIdAsync(original.Id))!.Name);
        provider.GetRequiredService<ITenantStoreCacheInvalidator>().InvalidateTenant(original.Id);
        Assert.Equal("after", (await cached.FindByIdAsync(original.Id))!.Name);
        Assert.Equal(2, store.FindByIdCalls);
    }

    [Fact]
    public async Task InvalidateTenant_RemovesCachedHostAliases()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "before");
        tenant.Domains.Add(new TenantDomain("tenant.example.com", isPrimary: true) { IsVerified = true, TenantId = tenant.Id });
        store.Set(tenant);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();

        Assert.Equal("before", (await cached.FindByHostAsync("TENANT.example.com."))!.Name);

        var updated = new TenantInfo(tenant.Id, "after");
        updated.Domains.Add(new TenantDomain("tenant.example.com", isPrimary: true) { IsVerified = true, TenantId = updated.Id });
        store.Set(updated);

        provider.GetRequiredService<ITenantStoreCacheInvalidator>().InvalidateTenant(tenant.Id);

        Assert.Equal("after", (await cached.FindByHostAsync("tenant.example.com"))!.Name);
        Assert.Equal(2, store.FindByHostCalls);
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsIndependentTenantSnapshots()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        tenant.Properties["tier"] = "standard";
        store.Set(tenant);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();

        var first = (await cached.FindByIdAsync(tenant.Id))!;
        first.Properties["tier"] = "mutated";

        var second = (await cached.FindByIdAsync(tenant.Id))!;

        Assert.Equal("standard", second.Properties["tier"]);
        Assert.Equal(1, store.FindByIdCalls);
    }

    [Fact]
    public async Task ConcurrentMisses_ShareOneStoreLookup()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        store.Set(tenant);
        store.LookupGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();

        var requests = Enumerable.Range(0, 12)
            .Select(_ => cached.FindByIdAsync(tenant.Id))
            .ToArray();

        await store.LookupStarted.Task;
        store.LookupGate.SetResult(true);

        var results = await Task.WhenAll(requests);

        Assert.All(results, result => Assert.Equal(tenant.Id, result!.Id));
        Assert.Equal(1, store.FindByIdCalls);
    }

    [Fact]
    public async Task ExpiredEntries_AreReloaded()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "before");
        store.Set(tenant);
        using var provider = CreateCachedStore(store, TimeSpan.FromMilliseconds(40));
        var cached = provider.GetRequiredService<ITenantStore>();

        Assert.Equal("before", (await cached.FindByIdAsync(tenant.Id))!.Name);
        store.Set(new TenantInfo(tenant.Id, "after"));
        await Task.Delay(100);

        Assert.Equal("after", (await cached.FindByIdAsync(tenant.Id))!.Name);
        Assert.Equal(2, store.FindByIdCalls);
    }

    [Fact]
    public async Task InvalidateDuringLookup_PreventsStaleResultFromRepopulatingCache()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "before");
        store.Set(tenant);
        store.LookupGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();
        var invalidator = provider.GetRequiredService<ITenantStoreCacheInvalidator>();

        var inFlight = cached.FindByIdAsync(tenant.Id);
        await store.LookupStarted.Task;
        store.Set(new TenantInfo(tenant.Id, "after"));
        invalidator.InvalidateTenant(tenant.Id);
        store.LookupGate.SetResult(true);

        Assert.Equal("before", (await inFlight)!.Name);
        Assert.Equal("after", (await cached.FindByIdAsync(tenant.Id))!.Name);
        Assert.Equal(2, store.FindByIdCalls);
    }

    [Fact]
    public async Task InvalidateHost_RefreshesOnlyTheCanonicalHostLookup()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "before");
        tenant.Domains.Add(new TenantDomain("tenant.example.com") { IsVerified = true, TenantId = tenant.Id });
        store.Set(tenant);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();

        Assert.Equal("before", (await cached.FindByHostAsync("tenant.example.com"))!.Name);

        var updated = new TenantInfo(tenant.Id, "after");
        updated.Domains.Add(new TenantDomain("tenant.example.com") { IsVerified = true, TenantId = updated.Id });
        store.Set(updated);
        provider.GetRequiredService<ITenantStoreCacheInvalidator>().InvalidateHost("TENANT.example.com.");

        Assert.Equal("after", (await cached.FindByHostAsync("tenant.example.com"))!.Name);
        Assert.Equal(2, store.FindByHostCalls);
    }

    [Fact]
    public async Task InvalidateAll_RefreshesIdExistenceAndRegistrySnapshots()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        store.Set(tenant);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();

        Assert.NotNull(await cached.FindByIdAsync(tenant.Id));
        Assert.True(await cached.ExistsAsync(tenant.Id));
        Assert.Single(await cached.GetAllAsync());

        store.Remove(tenant.Id);
        provider.GetRequiredService<ITenantStoreCacheInvalidator>().InvalidateAll();

        Assert.Null(await cached.FindByIdAsync(tenant.Id));
        Assert.False(await cached.ExistsAsync(tenant.Id));
        Assert.Empty(await cached.GetAllAsync());
    }

    [Fact]
    public async Task CancellationAwareLookup_ForwardsTokenToUnderlyingStore()
    {
        var store = new MutableTenantStore();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        store.Set(tenant);
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();
        using var cancellation = new CancellationTokenSource();

        Assert.NotNull(await cached.FindByIdAsync(tenant.Id, cancellation.Token));

        Assert.Equal(cancellation.Token, store.LastCancellationToken);
    }

    [Fact]
    public async Task CancellationAwareLookup_PropagatesCancellation()
    {
        var store = new MutableTenantStore();
        using var provider = CreateCachedStore(store);
        var cached = provider.GetRequiredService<ITenantStore>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cached.FindByIdAsync(Guid.NewGuid(), cancellation.Token));
        Assert.Equal(0, store.FindByIdCalls);
    }

    private static ServiceProvider CreateCachedStore(
        MutableTenantStore store,
        TimeSpan? expiration = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantStore>(store);
        services.AddTenantStoreCaching(options =>
            options.AbsoluteExpiration = expiration ?? TimeSpan.FromMinutes(1));
        return services.BuildServiceProvider();
    }

    private sealed class MutableTenantStore : ITenantStore
    {
        private readonly Dictionary<Guid, TenantInfo> _tenants = new();

        public int FindByIdCalls { get; private set; }
        public int FindByHostCalls { get; private set; }
        public CancellationToken? LastCancellationToken { get; private set; }
        public TaskCompletionSource<bool>? LookupGate { get; set; }
        public TaskCompletionSource<bool> LookupStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Set(TenantInfo tenant) => _tenants[tenant.Id] = tenant;

        public void Remove(Guid tenantId) => _tenants.Remove(tenantId);

        public async Task<TenantInfo?> FindByIdAsync(Guid id)
        {
            FindByIdCalls++;
            var result = _tenants.GetValueOrDefault(id);
            if (LookupGate is not null)
            {
                LookupStarted.TrySetResult(true);
                await LookupGate.Task;
            }
            return result;
        }

        public Task<TenantInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return FindByIdAsync(id);
        }

        public Task<TenantInfo?> FindByHostAsync(string host)
        {
            FindByHostCalls++;
            var result = _tenants.Values.SingleOrDefault(tenant =>
                tenant.Domains.Any(domain => domain.IsVerified && domain.Host == host));
            return Task.FromResult(result);
        }

        public Task<IEnumerable<TenantInfo>> GetAllAsync()
            => Task.FromResult<IEnumerable<TenantInfo>>(_tenants.Values.ToArray());

        public Task<bool> ExistsAsync(Guid id)
            => Task.FromResult(_tenants.ContainsKey(id));
    }
}
