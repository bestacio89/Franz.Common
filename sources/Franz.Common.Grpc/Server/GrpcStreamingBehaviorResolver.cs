using System;
using System.Collections.Concurrent;
using System.Linq;
using Franz.Common.Grpc.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Server;

/// <summary>Discovers, orders, and caches a streaming behavior pipeline per RPC message pair.</summary>
internal sealed class GrpcStreamingBehaviorResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<(Type Request, Type Response, Type Behavior), object> _cache = new();

    public GrpcStreamingBehaviorResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TBehavior[] Resolve<TRequest, TResponse, TBehavior>()
        where TRequest : class
        where TResponse : class
        where TBehavior : class, IOrderedGrpcStreamingBehavior
    {
        var key = (typeof(TRequest), typeof(TResponse), typeof(TBehavior));
        return (TBehavior[])_cache.GetOrAdd(key, _ => CreatePipeline<TRequest, TResponse, TBehavior>());
    }

    private TBehavior[] CreatePipeline<TRequest, TResponse, TBehavior>()
        where TRequest : class
        where TResponse : class
        where TBehavior : class, IOrderedGrpcStreamingBehavior
    {
        var behaviors = _serviceProvider.GetServices<TBehavior>().ToArray();
        if (behaviors.Length == 0)
        {
            return Array.Empty<TBehavior>();
        }

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
    }
}
