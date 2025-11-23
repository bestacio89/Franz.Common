using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Client.Interceptors;

namespace Franz.Common.Grpc.Client;

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

    // Canonical Franz ordering
    var ordered = all
        .OrderBy(b => BehaviorOrder(b))
        .ToArray();

    return ordered;
  }

  private static int BehaviorOrder<TRequest, TResponse>(IGrpcClientBehavior<TRequest, TResponse> b)
      where TRequest : class
      where TResponse : class
  {
    return b switch
    {
      ValidationClientBehavior<TRequest, TResponse> => 0,
      TenantResolutionClientBehavior<TRequest, TResponse> => 1,
      AuthorizationClientBehavior<TRequest, TResponse> => 2,
      LoggingClientBehavior<TRequest, TResponse> => 3,
      MetricClientBehavior<TRequest, TResponse> => 4,
      ExceptionMappingClientBehavior<TRequest, TResponse> => 5,
      _ => int.MaxValue
    };
  }
}
