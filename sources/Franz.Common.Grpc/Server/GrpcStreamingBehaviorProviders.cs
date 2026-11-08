using System.Collections.Concurrent;
using System.Linq;
using Franz.Common.Grpc.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Server;

/// <summary>
/// Internal shared discovery and ordering engine for the three distinct streaming RPC shapes.
/// </summary>
internal sealed class GrpcStreamingBehaviorResolver(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ConcurrentDictionary<(Type Request, Type Response, Type Behavior), object> _cache = new();

    public TBehavior[] Resolve<TRequest, TResponse, TBehavior>()
        where TRequest : class
        where TResponse : class
        where TBehavior : class, IOrderedGrpcStreamingBehavior
    {
        var key = (typeof(TRequest), typeof(TResponse), typeof(TBehavior));
        return (TBehavior[])_cache.GetOrAdd(key, _ =>
        {
            var behaviors = _serviceProvider.GetServices<TBehavior>().ToArray();
            if (behaviors.Length == 0)
                return Array.Empty<TBehavior>();

            var ordered = behaviors.OrderBy(behavior => behavior.Order).ToArray();
            var duplicateOrder = ordered
                .GroupBy(behavior => behavior.Order)
                .FirstOrDefault(group => group.Skip(1).Any());

            if (duplicateOrder is not null)
            {
                throw new InvalidOperationException(
                    $"Multiple gRPC streaming behaviors use the same execution order '{duplicateOrder.Key}' " +
                    $"for behavior contract '{typeof(TBehavior).FullName}', request '{typeof(TRequest).FullName}', " +
                    $"and response '{typeof(TResponse).FullName}'.");
            }

            return ordered;
        });
    }
}

/// <summary>Scoped provider for server-streaming behavior pipelines.</summary>
public sealed class GrpcServerStreamingBehaviorProvider : IGrpcServerStreamingBehaviorProvider
{
    private readonly GrpcStreamingBehaviorResolver _resolver;

    public GrpcServerStreamingBehaviorProvider(IServiceProvider serviceProvider)
        => _resolver = new GrpcStreamingBehaviorResolver(serviceProvider);

    public IGrpcServerStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
        => _resolver.Resolve<TRequest, TResponse, IGrpcServerStreamingBehavior<TRequest, TResponse>>();
}

/// <summary>Scoped provider for client-streaming behavior pipelines.</summary>
public sealed class GrpcClientStreamingBehaviorProvider : IGrpcClientStreamingBehaviorProvider
{
    private readonly GrpcStreamingBehaviorResolver _resolver;

    public GrpcClientStreamingBehaviorProvider(IServiceProvider serviceProvider)
        => _resolver = new GrpcStreamingBehaviorResolver(serviceProvider);

    public IGrpcClientStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
        => _resolver.Resolve<TRequest, TResponse, IGrpcClientStreamingBehavior<TRequest, TResponse>>();
}

/// <summary>Scoped provider for duplex-streaming behavior pipelines.</summary>
public sealed class GrpcDuplexStreamingBehaviorProvider : IGrpcDuplexStreamingBehaviorProvider
{
    private readonly GrpcStreamingBehaviorResolver _resolver;

    public GrpcDuplexStreamingBehaviorProvider(IServiceProvider serviceProvider)
        => _resolver = new GrpcStreamingBehaviorResolver(serviceProvider);

    public IGrpcDuplexStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
        => _resolver.Resolve<TRequest, TResponse, IGrpcDuplexStreamingBehavior<TRequest, TResponse>>();
}
