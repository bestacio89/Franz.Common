using Franz.Common.Http.MultiTenancy.Configuration;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Franz.Common.Http.MultiTenancy;

/// <summary>Fails host startup when required HTTP tenant boundary services are missing.</summary>
internal sealed class TenantResolutionStartupValidator(
    IServiceScopeFactory scopeFactory,
    IServiceProviderIsService serviceProviderIsService,
    IOptions<TenantResolutionOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = options.Value;

        if (configuration.RequireTenant)
        {
            using var scope = scopeFactory.CreateScope();
            if (!scope.ServiceProvider.GetServices<ITenantResolver>().Any())
            {
                throw new InvalidOperationException(
                    "HTTP multi-tenancy requires at least one ITenantResolver when TenantResolutionOptions.RequireTenant is enabled.");
            }
        }

        if (configuration.RequireAuthenticatedAuthorization &&
            !serviceProviderIsService.IsService(typeof(ITenantRequestAuthorizer)))
        {
            throw new InvalidOperationException(
                "HTTP multi-tenancy requires an ITenantRequestAuthorizer for authenticated requests. Register an application authorizer or opt in to AddTenantClaimsAuthorizer.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
