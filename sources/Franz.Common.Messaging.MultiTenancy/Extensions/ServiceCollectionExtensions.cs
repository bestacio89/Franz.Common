using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.Messaging.MultiTenancy.Configuration;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Franz.Common.Messaging.MultiTenancy.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingMultitenancyContext(
        this IServiceCollection services)
        => AddMessagingMultitenancyContext(services, configure: null);

    public static IServiceCollection AddMessagingMultitenancyContext(
        this IServiceCollection services,
        Action<TenantMessagingOptions>? configure)
    {
        services.Configure<MultiTenancyOptions>(options => options.Enabled = true);
        services.Configure<TenantMessagingOptions>(options => options.RequireTenantScope = true);
        services.Configure(configure ?? (_ => { }));
        services
            .AddNoDuplicateScoped<ITenantExecutionContextAccessor, ScopedTenantExecutionContextAccessor>()
            .AddNoDuplicateScoped<ITenantContextAccessor, TenantContextAccessor>()
            .AddNoDuplicateScoped<IDomainContextAccessor, DomainContextAccessor>()
            .AddNoDuplicateScoped<ITenantMessageContextInitializer, TenantMessageContextInitializer>();

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMessageBuilder, TenantMessageBuilder>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMessageBuilder, DomainMessageBuilder>());

        return services;
    }
}
