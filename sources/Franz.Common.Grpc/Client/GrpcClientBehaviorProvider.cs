using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Client.Interceptors;

namespace Franz.Common.Grpc.Client;

/// <summary>
/// Resolves and caches client behaviors in deterministic outermost-first order.
/// </summary>
public sealed class GrpcClientBehaviorProvider : IGrpcClientBehaviorProvider
{
  private readonly IServiceProvider _provider;
  private readonly ConcurrentDictionary<(Type, Type), object> _cache = new();

  public GrpcClientBehaviorProvider(IServiceProvider provider)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
  }

  public IGrpcClientBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
      where TRequest : class
      where TResponse : class
  {
    var key = (typeof(TRequest), typeof(TResponse));

    return (IGrpcClientBehavior<TRequest, TResponse>[])
        _cache.GetOrAdd(key, _ => CreatePipeline<TRequest, TResponse>());
  }

  private IGrpcClientBehavior<TRequest, TResponse>[] CreatePipeline<TRequest, TResponse>()
      where TRequest : class
      where TResponse : class
  {
    var all = _provider.GetServices<IGrpcClientBehavior<TRequest, TResponse>>()?.ToList()
              ?? new List<IGrpcClientBehavior<TRequest, TResponse>>();

    if (all.Count == 0)
      return Array.Empty<IGrpcClientBehavior<TRequest, TResponse>>();

    return all
        .OrderBy(BehaviorOrder)
        .ToArray();
  }

  private static int BehaviorOrder<TRequest, TResponse>(IGrpcClientBehavior<TRequest, TResponse> behavior)
      where TRequest : class
      where TResponse : class
  {
    return behavior switch
    {
      ExceptionMappingClientBehavior<TRequest, TResponse> => 0,
      ValidationClientBehavior<TRequest, TResponse> => 10,
      TenantResolutionClientBehavior<TRequest, TResponse> => 20,
      AuthorizationClientBehavior<TRequest, TResponse> => 30,
      LoggingClientBehavior<TRequest, TResponse> => 40,
      MetricClientBehavior<TRequest, TResponse> => 50,
      _ => int.MaxValue
    };
  }
}
