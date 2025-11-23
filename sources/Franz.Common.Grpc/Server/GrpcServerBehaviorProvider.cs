using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Interceptors;

namespace Franz.Common.Grpc.Server;

/// <summary>
/// Provides the ordered Franz server-side gRPC behaviors.
/// This class discovers and caches behavior pipelines per (Request/Response) pair.
/// </summary>
public sealed class GrpcServerBehaviorProvider : IGrpcServerBehaviorProvider
{
  private readonly IServiceProvider _provider;

  /// <summary>
  /// Cache: (RequestType, ResponseType) -> Ordered behavior array.
  /// </summary>
  private readonly ConcurrentDictionary<(Type, Type), object> _cache = new();

  public GrpcServerBehaviorProvider(IServiceProvider provider)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
  }

  /// <summary>
  /// Resolves the ordered list of server behaviors for TRequest/TResponse.
  /// </summary>
  public IGrpcServerBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
      where TRequest : class
      where TResponse : class
  {
    var key = (typeof(TRequest), typeof(TResponse));

    return (IGrpcServerBehavior<TRequest, TResponse>[])
        _cache.GetOrAdd(key, _ => CreatePipeline<TRequest, TResponse>());
  }

  /// <summary>
  /// Builds and caches the pipeline.
  /// </summary>
  private IGrpcServerBehavior<TRequest, TResponse>[] CreatePipeline<TRequest, TResponse>()
      where TRequest : class
      where TResponse : class
  {
    // Resolve all behaviors from DI (unordered)
    var all = _provider.GetServices<IGrpcServerBehavior<TRequest, TResponse>>()?.ToList()
              ?? new List<IGrpcServerBehavior<TRequest, TResponse>>();

    if (all.Count == 0)
      return Array.Empty<IGrpcServerBehavior<TRequest, TResponse>>();

    // Canonical Franz ordering
    var ordered = all
        .OrderBy(b => BehaviorOrder(b))
        .ToArray();

    return ordered;
  }

  /// <summary>
  /// Defines canonical Franz ordering across server behaviors.
  /// Lower value = execute earlier (outer behavior).
  /// </summary>
  private static int BehaviorOrder<TRequest, TResponse>(IGrpcServerBehavior<TRequest, TResponse> b)
      where TRequest : class
      where TResponse : class
  {
    return b switch
    {
      ValidationServerBehavior<TRequest, TResponse> => 0,
      TenantResolutionServerBehavior<TRequest, TResponse> => 1,
      AuthorizationServerBehavior<TRequest, TResponse> => 2,
      LoggingServerBehavior<TRequest, TResponse> => 3,
      MetricServerBehavior<TRequest, TResponse> => 4,
      ExceptionMappingServerBehavior<TRequest, TResponse> => 5,
      _ => int.MaxValue
    };
  }
}
