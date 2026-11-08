using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server;

/// <summary>Scoped provider for duplex-streaming behavior pipelines.</summary>
public sealed class GrpcDuplexStreamingBehaviorProvider : IGrpcDuplexStreamingBehaviorProvider
{
    private readonly GrpcStreamingBehaviorResolver _resolver;

    public GrpcDuplexStreamingBehaviorProvider(IServiceProvider serviceProvider)
    {
        _resolver = new GrpcStreamingBehaviorResolver(serviceProvider);
    }

    public IGrpcDuplexStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
        => _resolver.Resolve<TRequest, TResponse, IGrpcDuplexStreamingBehavior<TRequest, TResponse>>();
}
