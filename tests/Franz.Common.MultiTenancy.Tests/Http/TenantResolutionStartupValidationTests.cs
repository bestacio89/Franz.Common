using Franz.Common.Http.MultiTenancy.Extensions;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class TenantResolutionStartupValidationTests
{
    [Fact]
    public async Task HostStartup_RejectsMissingTenantResolver()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services => services.AddFranzMultiTenancy())
            .Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.StartAsync());

        Assert.Contains("at least one ITenantResolver", exception.Message);
    }

    [Fact]
    public async Task HostStartup_RejectsMissingAuthenticatedRequestAuthorizer()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddFranzMultiTenancy();
                services.AddSingleton<ITenantResolver, StartupTestTenantResolver>();
            })
            .Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.StartAsync());

        Assert.Contains("ITenantRequestAuthorizer", exception.Message);
    }

    [Fact]
    public async Task HostStartup_SucceedsWhenResolverAndClaimsAuthorizerAreRegistered()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddFranzMultiTenancy();
                services.AddSingleton<ITenantResolver, StartupTestTenantResolver>();
                services.AddTenantClaimsAuthorizer();
            })
            .Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    private sealed class StartupTestTenantResolver : ITenantResolver
    {
        public int Order => 0;

        public Task<TenantResolutionResult?> ResolveTenantAsync(object? context)
            => Task.FromResult<TenantResolutionResult?>(null);
    }
}
