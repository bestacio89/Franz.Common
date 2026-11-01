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
    public void SetCurrentTenantId_StoresServerEstablishedTenantInRequestItems()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        var accessor = new TenantContextAccessor(new HttpContextAccessor { HttpContext = context });

        accessor.SetCurrentTenantId(tenantId);

        Assert.Equal(tenantId, accessor.GetCurrentTenantId());
        Assert.False(context.Request.Headers.ContainsKey("X-Tenant-Id"));
    }
}
