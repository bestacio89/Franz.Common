using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Client;
using Franz.Common.Grpc.Configuration;
using Franz.Common.Grpc.Hosting.NoOp;
using Franz.Common.Grpc.Server;
using Franz.Common.Grpc.Server.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Franz.Common.Grpc.DependencyInjection;

/// <summary>
/// Dependency injection extensions for configuring Franz gRPC
/// on both client and server sides.
/// 
/// This intentionally avoids any hard dependency on ASP.NET Core
/// (no AddGrpc(), no routing, no endpoint mapping).
/// The host application is responsible for calling AddGrpc()/AddGrpcClient&lt;T&gt;.
/// </summary>
public static class GrpcServiceCollectionExtensions
{
  /// <summary>
  /// Registers the Franz server-side gRPC pipeline and behaviors.
  /// Does NOT call AddGrpc(); the host application must do that
  /// in its ASP.NET Core startup if needed.
  /// </summary>
  public static IServiceCollection AddFranzGrpcServer(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    // Bind global options
    services.Configure<FranzGrpcOptions>(
        configuration.GetSection("Franz:Grpc"));

    // Server behavior registration (Franz canonical order)
    services
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(ValidationServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(TenantResolutionServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(AuthorizationServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(LoggingServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(MetricServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(ExceptionMappingServerBehavior<,>));

    // Behavior provider (resolves ordered behaviors)
    services.AddNoDuplicateSingleton<IGrpcServerBehaviorProvider, GrpcServerBehaviorProvider>();

    return services;
  }

  /// <summary>
  /// Registers the Franz gRPC client-side pipeline.
  /// Does NOT call AddGrpcClient(); the host app must configure
  /// concrete typed gRPC clients using Grpc.Net.ClientFactory.
  /// </summary>
  public static IServiceCollection AddFranzGrpcClient(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    // Bind client-specific options
    services.Configure<FranzGrpcClientOptions>(
        configuration.GetSection("Franz:Grpc:Client"));

    // Behavior provider and factory
    services.AddNoDuplicateSingleton<IGrpcClientBehaviorProvider, GrpcClientBehaviorProvider>();
    services.AddNoDuplicateSingleton<IFranzGrpcClientFactory, FranzGrpcClientFactory>();

    return services;
  }

  /// <summary>
  /// Registers default (no-op) implementations for validation, authorization,
  /// logging, metrics, and tenant resolution so Franz can run without the host
  /// having to configure everything upfront.
  /// </summary>
  public static IServiceCollection AddFranzGrpcDefaults(
      this IServiceCollection services)
  {
    services.TryAddScoped<IFranzValidationEngine, NoOpValidationEngine>();
    services.TryAddScoped<IFranzAuthorizationService, NoOpAuthorizationService>();
    services.TryAddScoped<IFranzTenantResolver, NoOpTenantResolver>();
    services.TryAddScoped<IFranzGrpcLogger, NoOpGrpcLogger>();
    services.TryAddScoped<IFranzGrpcMetrics, NoOpGrpcMetrics>();

    return services;
  }
}
