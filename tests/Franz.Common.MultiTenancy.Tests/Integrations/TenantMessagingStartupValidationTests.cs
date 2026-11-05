using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.MultiTenancy.Configuration;
using Franz.Common.Messaging.MultiTenancy.Extensions;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class TenantMessagingStartupValidationTests
{
    [Fact]
    public void AddMessagingMultitenancyContext_RequiresTenantScopeByDefault()
    {
        var services = new ServiceCollection();
        services.AddMessagingMultitenancyContext();
        using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<TenantMessagingOptions>>().Value.RequireTenantScope);
    }

    [Fact]
    public void AddMessagingMultitenancyContext_AllowsExplicitTenantScopeOptOut()
    {
        var services = new ServiceCollection();
        services.AddMessagingMultitenancyContext(options => options.RequireTenantScope = false);
        using var provider = services.BuildServiceProvider();

        Assert.False(provider.GetRequiredService<IOptions<TenantMessagingOptions>>().Value.RequireTenantScope);
    }

    [Fact]
    public async Task GenericMessagingHostedService_ValidatesTenantScopeBeforeListening()
    {
        var services = new ServiceCollection();
        services.Configure<TenantMessagingOptions>(options => options.RequireTenantScope = true);
        using var provider = services.BuildServiceProvider();
        var hostedService = new MessagingHostedService(new StubListener(), provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));
    }

    private sealed class StubListener : IListener
    {
        public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }
        public Task Listen(CancellationToken stoppingToken = default) => Task.CompletedTask;
        public Task StopListenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

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
