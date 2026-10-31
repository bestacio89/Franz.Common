using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Franz.Common.Messaging;

namespace Franz.Common.Messaging.MultiTenancy.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingMultitenancyContext(this IServiceCollection services)
    {
        services
            .AddNoDuplicateScoped<ITenantExecutionContextAccessor, MessagingTenantExecutionContextAccessor>()
            .AddNoDuplicateScoped<ITenantContextAccessor, TenantContextAccessor>()
            .AddNoDuplicateScoped<IDomainContextAccessor, DomainContextAccessor>()
            .AddNoDuplicateScoped<ITenantMessageContextInitializer, TenantMessageContextInitializer>();

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMessageBuilder, TenantMessageBuilder>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMessageBuilder, DomainMessageBuilder>());

        return services;
    }
}
