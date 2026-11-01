using Franz.Common.Http.MultiTenancy.Resolvers;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class TenantResolutionCancellationTests
{
    [Fact]
    public async Task HeaderResolver_PropagatesCancellationToTenantStore()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var store = new CancellationAwareTenantStore(tenant);
        var resolver = new HeaderTenantResolver(store);
        var pipeline = new DefaultTenantResolutionPipeline([resolver]);
        using var cancellation = new CancellationTokenSource();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = tenant.Id.ToString();

        var result = await pipeline.ResolveAsync(context, cancellation.Token);

        Assert.True(result?.Succeeded);
        Assert.Equal(tenant.Id, result?.Tenant?.Id);
        Assert.Equal(cancellation.Token, store.CapturedToken);
    }

    private sealed class CancellationAwareTenantStore(TenantInfo tenant) : ITenantStore
    {
        public CancellationToken CapturedToken { get; private set; }

        public Task<TenantInfo?> FindByIdAsync(Guid id)
            => Task.FromResult<TenantInfo?>(id == tenant.Id ? tenant : null);

        public Task<TenantInfo?> FindByHostAsync(string host)
            => Task.FromResult<TenantInfo?>(null);

        public Task<IEnumerable<TenantInfo>> GetAllAsync()
            => Task.FromResult<IEnumerable<TenantInfo>>([tenant]);

        public Task<bool> ExistsAsync(Guid id)
            => Task.FromResult(id == tenant.Id);

        public Task<TenantInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            CapturedToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return FindByIdAsync(id);
        }
    }
}
