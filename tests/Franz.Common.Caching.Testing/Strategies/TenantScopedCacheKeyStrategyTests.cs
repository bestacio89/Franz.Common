using Franz.Common.Caching;
using Franz.Common.Caching.Estrategies;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.Caching.Tests.Strategies;

public sealed class TenantScopedCacheKeyStrategyTests
{
    private sealed class TenantContextAccessor(TenantExecutionContext? current)
        : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;
        public void Initialize(TenantExecutionContext context) => Current = context;
        public void Clear() => Current = null;
    }

    private static TenantExecutionContext CreateContext(Guid tenantId)
        => new(
            new TenantInfo(tenantId, $"tenant-{tenantId:N}"),
            domainId: null,
            TenantResolutionSource.JwtClaim,
            DateTimeOffset.UtcNow);

    [Fact]
    public void BuildKey_UsesValidatedTenantIdentity()
    {
        var tenantId = Guid.NewGuid();
        var strategy = new TenantScopedCacheKeyStrategy(
            new DefaultCacheKeyStrategy(),
            new TenantContextAccessor(CreateContext(tenantId)));

        var key = strategy.BuildKey(new { Id = 42 });

        Assert.StartsWith($"tenant:{tenantId:N}:", key);
    }

    [Fact]
    public void BuildKey_ThrowsWhenTenantContextIsMissing()
    {
        var strategy = new TenantScopedCacheKeyStrategy(
            new DefaultCacheKeyStrategy(),
            new TenantContextAccessor(null));

        Assert.Throws<InvalidOperationException>(() => strategy.BuildKey(new { Id = 42 }));
    }

    [Fact]
    public void BuildKey_IsolatesEquivalentRequestsAcrossTenants()
    {
        var request = new { Id = 42 };
        var tenantA = new TenantScopedCacheKeyStrategy(
            new DefaultCacheKeyStrategy(),
            new TenantContextAccessor(CreateContext(Guid.NewGuid())));
        var tenantB = new TenantScopedCacheKeyStrategy(
            new DefaultCacheKeyStrategy(),
            new TenantContextAccessor(CreateContext(Guid.NewGuid())));

        Assert.NotEqual(tenantA.BuildKey(request), tenantB.BuildKey(request));
    }
}
