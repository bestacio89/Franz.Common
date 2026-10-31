using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class TenantExecutionContextLifecycleTests
{
    [Fact]
    public void HttpAccessor_RejectsDoubleInitializationAndClearRemovesAllRequestState()
    {
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var accessor = new TenantExecutionContextAccessor(httpContextAccessor);
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var domainId = Guid.NewGuid();
        var executionContext = new TenantExecutionContext(
            tenant, domainId, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);

        accessor.Initialize(executionContext);

        Assert.Same(executionContext, accessor.Current);
        Assert.Throws<InvalidOperationException>(() => accessor.Initialize(executionContext));

        accessor.Clear();

        Assert.Null(accessor.Current);
        Assert.Empty(httpContext.Items);
    }

    [Fact]
    public void HttpAccessor_DoesNotLeakContextAcrossRequests()
    {
        var httpContextAccessor = new HttpContextAccessor();
        var accessor = new TenantExecutionContextAccessor(httpContextAccessor);
        var first = new TenantExecutionContext(
            new TenantInfo(Guid.NewGuid(), "first"), null,
            TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        var second = new TenantExecutionContext(
            new TenantInfo(Guid.NewGuid(), "second"), null,
            TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);

        httpContextAccessor.HttpContext = new DefaultHttpContext();
        accessor.Initialize(first);
        Assert.Equal(first.TenantId, accessor.Current?.TenantId);

        accessor.Clear();
        httpContextAccessor.HttpContext = new DefaultHttpContext();

        Assert.Null(accessor.Current);
        accessor.Initialize(second);
        Assert.Equal(second.TenantId, accessor.Current?.TenantId);
    }

    [Fact]
    public void MessagingAccessor_RequiresExplicitClearBeforeNextMessage()
    {
        var accessor = new MessagingTenantExecutionContextAccessor();
        var first = new TenantExecutionContext(
            new TenantInfo(Guid.NewGuid(), "first"), null,
            TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        var second = new TenantExecutionContext(
            new TenantInfo(Guid.NewGuid(), "second"), null,
            TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);

        accessor.Initialize(first);

        Assert.Throws<InvalidOperationException>(() => accessor.Initialize(second));
        Assert.Equal(first.TenantId, accessor.Current?.TenantId);

        accessor.Clear();
        Assert.Null(accessor.Current);

        accessor.Initialize(second);
        Assert.Equal(second.TenantId, accessor.Current?.TenantId);
    }
}
