using Franz.Common.Grpc.AspNetCore.Configuration;
using Franz.Common.Grpc.AspNetCore.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Franz.Common.Grpc.AspNetCore.DependencyInjection;

/// <summary>
/// Registers the ASP.NET Core transport adapter for the hosting-runtime-independent
/// Franz.Common.Grpc package. Call AddFranzGrpcServer first to register the core
/// execution pipeline and its behavior providers.
/// </summary>
public static class GrpcAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddFranzGrpcAspNetCore(
        this IServiceCollection services,
        Action<FranzGrpcAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        var options = services.AddOptions<FranzGrpcAspNetCoreOptions>();
        if (configure is not null)
            options.Configure(configure);

        options
            .Validate(
                value => !string.IsNullOrWhiteSpace(value.UserIdClaimType),
                "The gRPC authenticated user claim type must not be empty.")
            .ValidateOnStart();

        services.TryAddSingleton<FranzGrpcServerInterceptor>();
        services.AddGrpc(grpc => grpc.Interceptors.Add<FranzGrpcServerInterceptor>());

        return services;
    }
}
