using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.AspNetCore.Configuration;
using Franz.Common.Grpc.Server.Pipelines;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.AspNetCore.Interceptors;

/// <summary>
/// Applies the canonical Franz unary server pipeline at the ASP.NET Core gRPC
/// transport boundary. A fresh asynchronous scope owns all behavior dependencies
/// for the complete intercepted RPC.
/// </summary>
public sealed class FranzGrpcServerInterceptor : Interceptor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FranzGrpcAspNetCoreOptions _options;

    public FranzGrpcServerInterceptor(
        IServiceScopeFactory scopeFactory,
        IOptions<FranzGrpcAspNetCoreOptions> options)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IGrpcServerBehaviorProvider>();
        var callContext = CreateCallContext(context);
        var pipeline = FranzServerInteropPipeline.Build(
            provider.ResolveBehaviors<TRequest, TResponse>(),
            (message, _, _) => continuation(message, context));

        return await pipeline(request, callContext, context.CancellationToken).ConfigureAwait(false);
    }

    private GrpcCallContext CreateCallContext(ServerCallContext serverCallContext)
    {
        var principal = serverCallContext.GetHttpContext().User;
        var userId = principal.Identity?.IsAuthenticated == true
            ? principal.FindFirst(_options.UserIdClaimType)?.Value
            : null;

        return GrpcCallContext.FromServerCallContext(serverCallContext, userId: userId);
    }
}
