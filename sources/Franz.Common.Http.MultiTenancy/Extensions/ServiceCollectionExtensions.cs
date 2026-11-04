using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.Http.MultiTenancy.Configuration;
using Franz.Common.Http.MultiTenancy.Middleware;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Http.MultiTenancy.Extensions;

public static class MultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddFranzMultiTenancy(
        this IServiceCollection services,
        Action<TenantResolutionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<MultiTenancyOptions>(options => options.Enabled = true);
        services.AddHttpContextAccessor();
        services.AddOptions<TenantResolutionOptions>()
            .Configure(configure ?? (_ => { }))
            .Validate(
                options => options.ExcludedPaths.All(IsValidExcludedPath),
                "Excluded tenant-resolution paths must be absolute application paths, cannot be root, and cannot contain query strings or fragments.")
            .ValidateOnStart();

        services.TryAddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.TryAddScoped<IDomainContextAccessor, DomainContextAccessor>();
        services.TryAddScoped<ITenantExecutionContextAccessor, ScopedTenantExecutionContextAccessor>();
        services.TryAddScoped<ITenantResolutionPipeline, DefaultTenantResolutionPipeline>();
        services.TryAddScoped<IDomainResolutionPipeline, DefaultDomainResolutionPipeline>();
        services.TryAddScoped<TenantResolutionMiddleware>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, TenantResolutionStartupValidator>());
        return services;
    }

    /// <summary>
    /// Opts in to GUID tenant-claim membership authorization. Applications may
    /// instead register their own ITenantRequestAuthorizer implementation.
    /// </summary>
    public static IServiceCollection AddTenantClaimsAuthorizer(
        this IServiceCollection services,
        Action<TenantClaimsAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TenantClaimsAuthorizationOptions>()
            .Configure(configure ?? (_ => { }))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.TenantIdClaimType),
                "Tenant ID claim type must be configured.")
            .ValidateOnStart();
        services.TryAddScoped<ITenantRequestAuthorizer, ClaimsTenantRequestAuthorizer>();
        return services;
    }

    private static bool IsValidExcludedPath(string? path)
        => !string.IsNullOrWhiteSpace(path) &&
           path[0] == '/' &&
           path != "/" &&
           !path.Contains('?') &&
           !path.Contains('#') &&
           !path.Contains("//", StringComparison.Ordinal);
}
