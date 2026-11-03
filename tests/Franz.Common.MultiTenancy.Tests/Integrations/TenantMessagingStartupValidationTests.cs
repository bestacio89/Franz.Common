using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.MultiTenancy.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class TenantMessagingStartupValidationTests
{
    [Fact]
    public void RequiredTenantScope_FailsWhenInitializerIsNotRegistered()
    {
        var services = new ServiceCollection();
        services.Configure<TenantMessagingOptions>(options => options.RequireTenantScope = true);
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(
            () => TenantMessagingStartupValidation.Validate(provider));

        Assert.Contains(nameof(ITenantMessageContextInitializer), exception.Message);
    }

    [Fact]
    public void TenantScopeIsOptionalWhenRequirementIsDisabled()
    {
        var services = new ServiceCollection();
        services.Configure<TenantMessagingOptions>(options => options.RequireTenantScope = false);
        using var provider = services.BuildServiceProvider();

        TenantMessagingStartupValidation.Validate(provider);
    }
}
