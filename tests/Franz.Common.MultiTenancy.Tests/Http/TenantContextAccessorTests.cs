using Franz.Common.Http.MultiTenancy.Accessors;
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
    public void GetCurrentTenantId_ReturnsOnlyServerEstablishedItem()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        context.Items[HttpTenantContextKeysForTests.TenantId] = tenantId;
        var accessor = new TenantContextAccessor(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(tenantId, accessor.GetCurrentTenantId());
    }

    private static class HttpTenantContextKeysForTests
    {
        // The accessor deliberately exposes no mutable request-header contract.
        public static readonly object TenantId = typeof(TenantContextAccessor);
    }
}
