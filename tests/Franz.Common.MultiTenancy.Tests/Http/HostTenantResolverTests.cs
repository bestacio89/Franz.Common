using Franz.Common.Http.MultiTenancy.Resolvers;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy.Stores;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class HostTenantResolverTests
{
    [Fact]
    public async Task ResolveTenantAsync_RequiresVerifiedDomainOwnership()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        tenant.Domains.Add(new TenantDomain("tenant.example") { TenantId = tenant.Id });
        var resolver = new HostTenantResolver(new InMemoryTenantStore([tenant]));
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tenant.example");

        var result = await resolver.ResolveTenantAsync(context);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Null(result.Tenant);
    }

    [Fact]
    public async Task ResolveTenantAsync_ResolvesVerifiedCanonicalHost()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        tenant.Domains.Add(new TenantDomain("TENANT.EXAMPLE.") { TenantId = tenant.Id, IsVerified = true });
        var resolver = new HostTenantResolver(new InMemoryTenantStore([tenant]));
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tenant.example");

        var result = await resolver.ResolveTenantAsync(context);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(tenant.Id, result.Tenant?.Id);
    }
}
