using System.Collections.Concurrent;
using System.Linq;
using Franz.Common.Grpc.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Server;

/// <summary>
/// Shared scoped resolution and ordering implementation for the three distinct streaming RPC shapes.
/// The behavior contract remains specific to each shape; only discovery and ordering are shared.
/// </summary>
internal abstract class GrpcStreamingBehaviorProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<(Type Request, Type Response, Type Behavior), object> _cache = new();

    protected GrpcStreamingBehaviorProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected TBehavior[] Resolve<TRequest, TResponse, TBehavior>()
        where TRequest : class
        where TResponse : class
        where TBehavior : class, IOrderedGrpcStreamingBehavior
    {
        var key = (typeof(TRequest), typeof(TResponse), typeof(TBehavior));
        return (TBehavior[])_cache.GetOrAdd(key, _ => Create<TBehavior>());

        TBehavior[] Create<T>() where T : class, IOrderedGrpcStreamingBehavior
        {
            var behaviors = _serviceProvider.GetServices<T>().ToArray();
            if (behaviors.Length == 0)
                return Array.Empty<T>();

            var ordered = behaviors.OrderBy(behavior => behavior.Order).ToArray();
            var duplicateOrder = ordered
                .GroupBy(behavior => behavior.Order)
                .FirstOrDefault(group => group.Skip(1).Any());

            if (duplicateOrder is not null)
            {
                throw new InvalidOperationException(
                    $"Multiple gRPC streaming behaviors use the same execution order '{duplicateOrder.Key}' " +
                    $"for behavior contract '{typeof(T).FullName}', request '{typeof(TRequest).FullName}', " +
                    $"and response '{typeof(TResponse).FullName}'.");
            }

            return ordered;
        }
    }
}

/// <summary>Scoped provider for server-streaming behavior pipelines.</summary>
public sealed class GrpcServerStreamingBehaviorProvider(IServiceProvider serviceProvider)
    : GrpcStreamingBehaviorProvider(serviceProvider), IGrpcServerStreamingBehaviorProvider
{
    public IGrpcServerStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        => Resolve<TRequest, TResponse, IGrpcServerStreamingBehavior<TRequest, TResponse>>()
        where TRequest : class
        where TResponse : class;
}

/// <summary>Scoped provider for client-streaming behavior pipelines.</summary>
public sealed class GrpcClientStreamingBehaviorProvider(IServiceProvider serviceProvider)
    : GrpcStreamingBehaviorProvider(serviceProvider), IGrpcClientStreamingBehaviorProvider
{
    public IGrpcClientStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        => Resolve<TRequest, TResponse, IGrpcClientStreamingBehavior<TRequest, TResponse>>()
        where TRequest : class
        where TResponse : class;
}

/// <summary>Scoped provider for duplex-streaming behavior pipelines.</summary>
public sealed class GrpcDuplexStreamingBehaviorProvider(IServiceProvider serviceProvider)
    : GrpcStreamingBehaviorProvider(serviceProvider), IGrpcDuplexStreamingBehaviorProvider
{
    public IGrpcDuplexStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        => Resolve<TRequest, TResponse, IGrpcDuplexStreamingBehavior<TRequest, TResponse>>()
        where TRequest : class
        where TResponse : class;
}
