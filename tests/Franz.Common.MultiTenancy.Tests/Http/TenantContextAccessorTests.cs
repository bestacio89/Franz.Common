using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class TenantContextAccessorTests
{
    [Fact]
    public void GetCurrentTenantId_DoesNotTrustCallerSuppliedHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();
        var accessor = new TenantContextAccessor(new HttpContextAccessor { HttpContext = context });

        Assert.Null(accessor.GetCurrentTenantId());
    }

    [Fact]
    public void SetCurrentTenantId_StoresServerEstablishedTenantInRequestItems()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        var accessor = new TenantContextAccessor(new HttpContextAccessor { HttpContext = context });

        accessor.SetCurrentTenantId(tenantId);

        Assert.Equal(tenantId, accessor.GetCurrentTenantId());
        Assert.False(context.Request.Headers.ContainsKey("X-Tenant-Id"));
    }

    [Fact]
    public void SetCurrentTenantId_CannotReplaceValidatedExecutionContext()
    {
        var context = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var executionAccessor = new TenantExecutionContextAccessor(httpContextAccessor);
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        executionAccessor.Initialize(new TenantExecutionContext(
            tenant,
            null,
            TenantResolutionSource.Host,
            DateTimeOffset.UtcNow));
        var accessor = new TenantContextAccessor(httpContextAccessor);

        Assert.Throws<InvalidOperationException>(() => accessor.SetCurrentTenantId(Guid.NewGuid()));
        Assert.Equal(tenant.Id, accessor.GetCurrentTenantId());
    }

    [Fact]
    public void SetCurrentDomainId_CannotReplaceValidatedExecutionContext()
    {
        var context = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var executionAccessor = new TenantExecutionContextAccessor(httpContextAccessor);
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var domainId = Guid.NewGuid();
        executionAccessor.Initialize(new TenantExecutionContext(
            tenant,
            domainId,
            TenantResolutionSource.Host,
            DateTimeOffset.UtcNow));
        var accessor = new DomainContextAccessor(httpContextAccessor);

        Assert.Throws<InvalidOperationException>(() => accessor.SetCurrentDomainId(Guid.NewGuid()));
        Assert.Equal(domainId, accessor.GetCurrentDomainId());
    }
}
