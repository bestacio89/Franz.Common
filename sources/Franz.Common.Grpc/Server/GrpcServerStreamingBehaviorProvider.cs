using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server;

/// <summary>Scoped provider for server-streaming behavior pipelines.</summary>
public sealed class GrpcServerStreamingBehaviorProvider : IGrpcServerStreamingBehaviorProvider
{
    private readonly GrpcStreamingBehaviorResolver _resolver;

    public GrpcServerStreamingBehaviorProvider(IServiceProvider serviceProvider)
    {
        _resolver = new GrpcStreamingBehaviorResolver(serviceProvider);
    }

    public IGrpcServerStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
        => _resolver.Resolve<TRequest, TResponse, IGrpcServerStreamingBehavior<TRequest, TResponse>>();
}
