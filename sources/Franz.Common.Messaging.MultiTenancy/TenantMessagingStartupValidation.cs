using Franz.Common.Messaging.MultiTenancy.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.MultiTenancy;

/// <summary>Validates tenant execution requirements before a hosted consumer starts listening.</summary>
public static class TenantMessagingStartupValidation
{
    public static void Validate(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.GetService<IOptions<TenantMessagingOptions>>()?.Value;
        if (options?.RequireTenantScope != true)
            return;

        using var scope = services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<ITenantMessageContextInitializer>();
    }
}
