using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy.TenantResolution;

namespace Franz.Common.MultiTenancy.Tests.Core;

public sealed class TenantResolutionPipelineTests
{
    [Fact]
    public async Task ResolveAsync_WhenResolversDisagree_ReturnsFailure()
    {
        var first = new TenantInfo(Guid.NewGuid(), "first");
        var second = new TenantInfo(Guid.NewGuid(), "second");
        var pipeline = new DefaultTenantResolutionPipeline(
        [
            new StubResolver(10, TenantResolutionResult.Success(first, TenantResolutionSource.Host)),
            new StubResolver(20, TenantResolutionResult.Success(second, TenantResolutionSource.JwtClaim))
        ]);

        var result = await pipeline.ResolveAsync();

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Null(result.Tenant);
        Assert.Contains("Conflicting", result.Message);
    }

    [Fact]
    public async Task ResolveAsync_WhenResolversAgree_ReturnsTenant()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var pipeline = new DefaultTenantResolutionPipeline(
        [
            new StubResolver(10, TenantResolutionResult.Success(tenant, TenantResolutionSource.Host)),
            new StubResolver(20, TenantResolutionResult.Success(tenant, TenantResolutionSource.JwtClaim))
        ]);

        var result = await pipeline.ResolveAsync();

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(tenant.Id, result.Tenant?.Id);
    }

    [Fact]
    public async Task ResolveAsync_PropagatesCancellationToResolvers()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        using var cancellation = new CancellationTokenSource();
        var resolver = new CancellationAwareStubResolver(tenant);
        var pipeline = new DefaultTenantResolutionPipeline([resolver]);

        await pipeline.ResolveAsync(new object(), cancellation.Token);

        Assert.Equal(cancellation.Token, resolver.CapturedToken);
    }

    [Fact]
    public async Task ResolveAsync_ThrowsWhenAlreadyCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var pipeline = new DefaultTenantResolutionPipeline([]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pipeline.ResolveAsync(new object(), cancellation.Token));
    }

    private sealed class StubResolver(int order, TenantResolutionResult? result) : ITenantResolver
    {
        public int Order => order;
        public Task<TenantResolutionResult?> ResolveTenantAsync(object? context) => Task.FromResult(result);
    }

    private sealed class CancellationAwareStubResolver(TenantInfo tenant) : ITenantResolver
    {
        public int Order => 0;
        public CancellationToken CapturedToken { get; private set; }

        public Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
            => Task.FromResult<TenantResolutionResult?>(
                TenantResolutionResult.Success(tenant, TenantResolutionSource.Host));

        public Task<TenantResolutionResult?> ResolveTenantAsync(
            object? context,
            CancellationToken cancellationToken)
        {
            CapturedToken = cancellationToken;
            return ResolveTenantAsync(context);
        }
    }
}
