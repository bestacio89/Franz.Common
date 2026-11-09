using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Client.Interceptors;
using Franz.Common.Grpc.Configuration;
using Franz.Common.Grpc.Client;
using Franz.Common.Grpc.Hosting;
using Franz.Common.Grpc.Hosting.NoOp;
using Franz.Common.Grpc.Hosting.Security;
using Franz.Common.MultiTenancy;
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
    services.TryAddScoped<ITenantExecutionContextAccessor, GrpcTenantExecutionContextAccessor>();

    // Bind global options
    services.AddOptions<FranzGrpcOptions>()
        .Bind(configuration.GetSection("Franz:Grpc"))
        .ValidateOnStart();

    // Server behavior registration (Franz canonical order)
    services
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(ValidationServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(TenantResolutionServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(AuthorizationServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(LoggingServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(MetricServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerBehavior<,>), typeof(ExceptionMappingServerBehavior<,>));

    // Exception mapping encloses the complete lifetime of every streaming RPC shape.
    services
        .AddNoDuplicateScoped(typeof(IGrpcServerStreamingBehavior<,>), typeof(ExceptionMappingStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientStreamingBehavior<,>), typeof(ExceptionMappingStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcDuplexStreamingBehavior<,>), typeof(ExceptionMappingStreamingBehavior<,>));

    // Apply tenant resolution before authorization for every server-side streaming RPC shape.
    services
        .AddNoDuplicateScoped(typeof(IGrpcServerStreamingBehavior<,>), typeof(TenantResolutionServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientStreamingBehavior<,>), typeof(TenantResolutionServerBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcDuplexStreamingBehavior<,>), typeof(TenantResolutionServerBehavior<,>));

    // Apply the same configured authorization policy to all server-side streaming RPC shapes.
    services
        .AddNoDuplicateScoped(typeof(IGrpcServerStreamingBehavior<,>), typeof(AuthorizationStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientStreamingBehavior<,>), typeof(AuthorizationStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcDuplexStreamingBehavior<,>), typeof(AuthorizationStreamingBehavior<,>));

    // Streaming lifecycle logging and metrics remain active through full enumeration.
    services
        .AddNoDuplicateScoped(typeof(IGrpcServerStreamingBehavior<,>), typeof(LoggingStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientStreamingBehavior<,>), typeof(LoggingStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcDuplexStreamingBehavior<,>), typeof(LoggingStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcServerStreamingBehavior<,>), typeof(MetricStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientStreamingBehavior<,>), typeof(MetricStreamingBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcDuplexStreamingBehavior<,>), typeof(MetricStreamingBehavior<,>));

    // Unary and streaming behavior providers are scoped to the RPC service lifetime.
    services.AddNoDuplicateScoped<IGrpcServerBehaviorProvider, GrpcServerBehaviorProvider>();
    services.AddNoDuplicateScoped<IGrpcServerStreamingBehaviorProvider, GrpcServerStreamingBehaviorProvider>();
    services.AddNoDuplicateScoped<IGrpcClientStreamingBehaviorProvider, GrpcClientStreamingBehaviorProvider>();
    services.AddNoDuplicateScoped<IGrpcDuplexStreamingBehaviorProvider, GrpcDuplexStreamingBehaviorProvider>();

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
    services.TryAddScoped<ITenantExecutionContextAccessor, GrpcTenantExecutionContextAccessor>();

    // Bind client-specific options
    services.AddOptions<FranzGrpcClientOptions>()
        .Bind(configuration.GetSection("Franz:Grpc:Client"))
        .Validate(
            options => options.DefaultClientTimeout > TimeSpan.Zero
                && options.DefaultClientTimeout <= TimeSpan.FromDays(365),
            "The default gRPC client timeout must be greater than zero and no longer than 365 days.")
        .ValidateOnStart();

    // Client behaviors are executable defaults. Authorization remains opt-in because
    // the secure default authorization service intentionally denies every call.
    services.TryAddScoped<IFranzValidationEngine, NoOpValidationEngine>();
    services.TryAddScoped<IFranzGrpcLogger, NoOpGrpcLogger>();
    services.TryAddScoped<IFranzGrpcMetrics, NoOpGrpcMetrics>();

    services
        .AddNoDuplicateScoped(typeof(IGrpcClientBehavior<,>), typeof(ExceptionMappingClientBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientBehavior<,>), typeof(ValidationClientBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientBehavior<,>), typeof(TenantResolutionClientBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientBehavior<,>), typeof(LoggingClientBehavior<,>))
        .AddNoDuplicateScoped(typeof(IGrpcClientBehavior<,>), typeof(MetricClientBehavior<,>));

    // The host registers AuthorizationClientBehavior explicitly when a client-side
    // authorization policy is required; it is never silently made permissive.
    services.AddNoDuplicateScoped<IGrpcClientBehaviorProvider, GrpcClientBehaviorProvider>();

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
    // Authorization is fail-closed by default. Hosts must register an explicit
    // authorization policy to permit calls; allow-all is never selected implicitly.
    services.TryAddScoped<IFranzAuthorizationService, DenyAllAuthorizationService>();
    services.TryAddScoped<IFranzTenantResolver, NoOpTenantResolver>();
    services.TryAddScoped<IFranzGrpcLogger, NoOpGrpcLogger>();
    services.TryAddScoped<IFranzGrpcMetrics, NoOpGrpcMetrics>();

    return services;
  }
}
