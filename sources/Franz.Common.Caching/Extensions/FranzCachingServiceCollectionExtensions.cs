using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Caching.Extensions
{
  public static class FranzCachingServiceCollectionExtensions
  {
    #region Cache Providers

    public static IServiceCollection AddFranzMemoryCaching(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
      services.AddMemoryCache();
      services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();
      if (configure != null)
        services.Configure(configure);
      return services;
    }

    public static IServiceCollection AddFranzDistributedCaching<TDistributedCache>(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
        where TDistributedCache : class, IDistributedCache
    {
      services.AddSingleton<IDistributedCache, TDistributedCache>();
      RegisterCacheProvider(services, typeof(DistributedCacheProvider), configure);
      return services;
    }

    public static IServiceCollection AddFranzRedisCaching(
        this IServiceCollection services,
        string connectionString,
        int database = 0,
        Action<CacheOptions>? configure = null)
    {
      services.AddSingleton<IConnectionMultiplexer>(_ =>
          ConnectionMultiplexer.Connect(
              new StackExchange.Redis.ConfigurationOptions
              {
                EndPoints = { connectionString },
                AbortOnConnectFail = false,
                DefaultDatabase = database
              }));

      RegisterCacheProvider(services, typeof(RedisCacheProvider), configure);
      return services;
    }

    public static IServiceCollection AddFranzRedisCaching(
        this IServiceCollection services,
        Func<IServiceProvider, IConnectionMultiplexer> multiplexerFactory,
        Action<CacheOptions>? configure = null)
    {
      services.AddSingleton(multiplexerFactory);
      RegisterCacheProvider(services, typeof(RedisCacheProvider), configure);
      return services;
    }

    public static IServiceCollection AddFranzCaching(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
        => services.AddFranzMemoryCaching(configure);

    private static void RegisterCacheProvider(IServiceCollection services, Type providerType, Action<CacheOptions>? configure)
    {
      services.TryAddSingleton(typeof(ICacheProvider), providerType);
      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();
      if (configure != null)
        services.Configure(configure);
    }

    #endregion

    #region Mediator Pipeline

    public static IServiceCollection AddFranzMediatorCaching(
        this IServiceCollection services,
        Action<MediatorCachingOptions>? configure = null)
    {
      if (configure != null)
        services.Configure(configure);

      services.AddScoped(typeof(CachingPipeline<,>));
      return services;
    }

    #endregion

    #region Observability / Observers

    public static IServiceCollection AddObservableCaching(this IServiceCollection services)
    {
      var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICacheProvider));
      if (descriptor == null)
        throw new InvalidOperationException(
            "ICacheProvider must be registered before calling AddObservableCaching().");

      services.Remove(descriptor);

      services.Add(new ServiceDescriptor(
          typeof(ICacheProvider),
          sp =>
          {
            var inner = descriptor.ImplementationInstance != null
                      ? (ICacheProvider)descriptor.ImplementationInstance
                      : descriptor.ImplementationFactory != null
                          ? (ICacheProvider)descriptor.ImplementationFactory(sp)
                          : (ICacheProvider)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);

            var observers = sp.GetServices<ICacheObserver>();
            return new ObservableCacheProvider(inner, observers);
          },
          descriptor.Lifetime));

      return services;
    }

    public static IServiceCollection AddMetricsCacheObserver(this IServiceCollection services)
        => AddObserver<MetricsCacheObserver>(services);

    public static IServiceCollection AddLoggingCacheObserver(this IServiceCollection services)
        => AddObserver<LoggingCacheObserver>(services);

    public static IServiceCollection AddLoggingMetricsCacheObserver(this IServiceCollection services)
        => AddObserver<LoggingMetricsObserver>(services);

    public static IServiceCollection AddExcelMetricsCacheObserver(this IServiceCollection services)
        => AddObserver<ExcelCacheObserver>(services);

    public static IServiceCollection AddCompositeCacheObserver(this IServiceCollection services)
    {
      services.AddSingleton<ICacheObserver>(sp =>
      {
        var observers = sp.GetServices<ICacheObserver>();
        return new CompositeCacheObserver(observers);
      });

      return services;
    }

    private static IServiceCollection AddObserver<T>(IServiceCollection services)
        where T : class, ICacheObserver
    {
      services.TryAddSingleton<T>();
      services.TryAddEnumerable(ServiceDescriptor.Singleton<ICacheObserver, T>());
      return services;
    }

    #endregion
  }

  #region Composite Observer

  public class CompositeCacheObserver : ICacheObserver
  {
    private readonly ICacheObserver[] _observers;

    public CompositeCacheObserver(IEnumerable<ICacheObserver> observers)
    {
      _observers = observers.ToArray();
    }

    public void OnCacheSet(CacheEntryDescriptor entry) =>
        Array.ForEach(_observers, o => o.OnCacheSet(entry));

    public void OnCacheHit(CacheAccessDescriptor access) =>
        Array.ForEach(_observers, o => o.OnCacheHit(access));

    public void OnCacheRemove(string key) =>
        Array.ForEach(_observers, o => o.OnCacheRemove(key));

    public void OnCacheRemoveByTag(string tag) =>
        Array.ForEach(_observers, o => o.OnCacheRemoveByTag(tag));
  }

  #endregion
}
