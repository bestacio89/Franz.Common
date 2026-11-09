using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server;

/// <summary>
/// Provides the ordered Franz server-side gRPC behaviors.
/// This class discovers and caches behavior pipelines per (Request/Response) pair.
/// </summary>
public sealed class GrpcServerBehaviorProvider : IGrpcServerBehaviorProvider
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<(Type, Type), object> _cache = new();

    public GrpcServerBehaviorProvider(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public IGrpcServerBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
    {
        var key = (typeof(TRequest), typeof(TResponse));

        return (IGrpcServerBehavior<TRequest, TResponse>[])
            _cache.GetOrAdd(key, _ => CreatePipeline<TRequest, TResponse>());
    }

    private IGrpcServerBehavior<TRequest, TResponse>[] CreatePipeline<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
    {
        var all = _provider.GetServices<IGrpcServerBehavior<TRequest, TResponse>>()?.ToList()
                  ?? new List<IGrpcServerBehavior<TRequest, TResponse>>();

        if (all.Count == 0)
            return Array.Empty<IGrpcServerBehavior<TRequest, TResponse>>();

        var ordered = all
            .OrderBy(behavior => behavior.Order)
            .ToArray();

        var duplicateOrder = ordered
            .GroupBy(behavior => behavior.Order)
            .FirstOrDefault(group => group.Skip(1).Any());

        if (duplicateOrder is not null)
        {
            throw new InvalidOperationException(
                $"Multiple gRPC server behaviors use the same execution order '{duplicateOrder.Key}' " +
                $"for request '{typeof(TRequest).FullName}' and response '{typeof(TResponse).FullName}'.");
        }

        return ordered;
    }
}
