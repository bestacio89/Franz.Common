using Franz.Common.Http.MultiTenancy.Extensions;
using Franz.Common.Messaging.MultiTenancy.Extensions;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class HybridTenantExecutionContextRegistrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HttpAndMessagingRegistrations_UseOneScopedAccessorRegardlessOfOrder(bool httpFirst)
    {
        var services = new ServiceCollection();

        if (httpFirst)
        {
            services.AddFranzMultiTenancy();
            services.AddMessagingMultitenancyContext();
        }
        else
        {
            services.AddMessagingMultitenancyContext();
            services.AddFranzMultiTenancy();
        }

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstAccessor = firstScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();
        var secondAccessor = secondScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();

        Assert.IsType<ScopedTenantExecutionContextAccessor>(firstAccessor);
        Assert.IsType<ScopedTenantExecutionContextAccessor>(secondAccessor);
        Assert.NotSame(firstAccessor, secondAccessor);

        var firstTenant = new TenantExecutionContext(
            new TenantInfo(Guid.NewGuid(), "http-request"),
            null,
            TenantResolutionSource.Unknown,
            DateTimeOffset.UtcNow);
        var secondTenant = new TenantExecutionContext(
            new TenantInfo(Guid.NewGuid(), "message"),
            null,
            TenantResolutionSource.Header,
            DateTimeOffset.UtcNow);

        firstAccessor.Initialize(firstTenant);
        secondAccessor.Initialize(secondTenant);

        Assert.Same(firstTenant, firstAccessor.Current);
        Assert.Same(secondTenant, secondAccessor.Current);
        Assert.NotEqual(firstAccessor.Current!.TenantId, secondAccessor.Current!.TenantId);
    }
}
