using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.Http.MultiTenancy.Configuration;
using Franz.Common.Http.MultiTenancy.Middleware;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Franz.Common.Http.MultiTenancy.Extensions;

public static class MultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddFranzMultiTenancy(
        this IServiceCollection services,
        Action<TenantResolutionOptions>? configure = null)
    {
        services.AddHttpContextAccessor();
        services.Configure(configure ?? (_ => { }));
        services.TryAddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.TryAddScoped<IDomainContextAccessor, DomainContextAccessor>();
        services.TryAddScoped<ITenantExecutionContextAccessor, TenantExecutionContextAccessor>();
        services.TryAddScoped<ITenantResolutionPipeline, DefaultTenantResolutionPipeline>();
        services.TryAddScoped<IDomainResolutionPipeline, DefaultDomainResolutionPipeline>();
        return services;
    }
}
