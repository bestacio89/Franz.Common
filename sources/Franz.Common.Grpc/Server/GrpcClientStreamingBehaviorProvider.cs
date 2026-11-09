using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server;

/// <summary>Scoped provider for client-streaming behavior pipelines.</summary>
public sealed class GrpcClientStreamingBehaviorProvider : IGrpcClientStreamingBehaviorProvider
{
    private readonly GrpcStreamingBehaviorResolver _resolver;

    public GrpcClientStreamingBehaviorProvider(IServiceProvider serviceProvider)
    {
        _resolver = new GrpcStreamingBehaviorResolver(serviceProvider);
    }

    public IGrpcClientStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
        => _resolver.Resolve<TRequest, TResponse, IGrpcClientStreamingBehavior<TRequest, TResponse>>();
}
