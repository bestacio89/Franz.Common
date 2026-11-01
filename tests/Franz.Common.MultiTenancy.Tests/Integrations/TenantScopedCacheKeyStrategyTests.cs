using Franz.Common.Caching;
using Franz.Common.Caching.Estrategies;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class TenantScopedCacheKeyStrategyTests
{
    [Fact]
    public void GetKey_SeparatesIdenticalRequestsAcrossTenants()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var strategyA = new TenantScopedCacheKeyStrategy(new DefaultCacheKeyStrategy(), new StubAccessor(CreateContext(tenantA)));
        var strategyB = new TenantScopedCacheKeyStrategy(new DefaultCacheKeyStrategy(), new StubAccessor(CreateContext(tenantB)));

        var keyA = strategyA.BuildKey(new { Id = 42 });
        var keyB = strategyB.BuildKey(new { Id = 42 });

        Assert.NotEqual(keyA, keyB);
        Assert.StartsWith($"tenant:{tenantA:N}:", keyA);
        Assert.StartsWith($"tenant:{tenantB:N}:", keyB);
    }

    [Fact]
    public void GetKey_UsesSeparateGlobalNamespaceWithoutTenantContext()
    {
        var strategy = new TenantScopedCacheKeyStrategy(new DefaultCacheKeyStrategy(), new StubAccessor(null));

        Assert.StartsWith("global:", strategy.GetKey(new { Id = 42 }));
    }

    private static TenantExecutionContext CreateContext(Guid tenantId)
        => new(new TenantInfo(tenantId, "tenant"), null, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);

    private sealed class StubAccessor(TenantExecutionContext? current) : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;
        public void Initialize(TenantExecutionContext context) => Current = context;
        public void Clear() => Current = null;
    }
}
