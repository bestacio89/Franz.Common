using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Franz.Common.Mediator.Pipelines.Core;
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
      services.TryAddSingleton<ICacheProvider, DistributedCacheProvider>();
      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();

      if (configure != null)
        services.Configure(configure);

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

      services.TryAddSingleton<ICacheProvider, RedisCacheProvider>();

      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();

      if (configure != null)
        services.Configure(configure);

      return services;
    }

    public static IServiceCollection AddFranzRedisCaching(
        this IServiceCollection services,
        Func<IServiceProvider, IConnectionMultiplexer> multiplexerFactory,
        Action<CacheOptions>? configure = null)
    {
      services.AddSingleton<IConnectionMultiplexer>(multiplexerFactory);

      services.TryAddSingleton<ICacheProvider, RedisCacheProvider>();

      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();

      if (configure != null)
        services.Configure(configure);

      return services;
    }

    public static IServiceCollection AddFranzCaching(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
        => services.AddFranzMemoryCaching(configure);

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

    /// <summary>
    /// Decorates the cache provider to support observers.
    /// </summary>
    public static IServiceCollection AddObservableCaching(this IServiceCollection services)
    {
      // Find the existing ICacheProvider registration
      var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICacheProvider));
      if (descriptor == null)
      {
        throw new InvalidOperationException(
          "ICacheProvider must be registered before calling AddObservableCaching(). " +
          "Call AddFranzMemoryCaching(), AddFranzRedisCaching(), or AddFranzDistributedCaching() first.");
      }

      // Remove the original registration
      services.Remove(descriptor);

      // Create a new registration that resolves the inner provider and wraps it
      services.Add(new ServiceDescriptor(
        typeof(ICacheProvider),
        sp =>
        {
          // Resolve the inner provider based on the original registration type
          ICacheProvider innerProvider;

          if (descriptor.ImplementationInstance != null)
          {
            // Use the existing instance
            innerProvider = (ICacheProvider)descriptor.ImplementationInstance;
          }
          else if (descriptor.ImplementationFactory != null)
          {
            // Call the factory to create the instance
            innerProvider = (ICacheProvider)descriptor.ImplementationFactory(sp);
          }
          else if (descriptor.ImplementationType != null)
          {
            // Create instance using ActivatorUtilities to resolve dependencies
            innerProvider = (ICacheProvider)ActivatorUtilities.CreateInstance(
              sp,
              descriptor.ImplementationType);
          }
          else
          {
            throw new InvalidOperationException(
              "Unable to resolve inner cache provider - unknown registration type.");
          }

          // Get all registered observers and wrap the provider
          var observers = sp.GetServices<ICacheObserver>();
          return new ObservableCacheProvider(innerProvider, observers);
        },
        descriptor.Lifetime));

      return services;
    }

    /// <summary>
    /// Registers the metrics-only cache observer.
    /// </summary>
    public static IServiceCollection AddMetricsCacheObserver(this IServiceCollection services)
    {
      services.TryAddSingleton<MetricsCacheObserver>();
      services.TryAddEnumerable(ServiceDescriptor.Singleton<ICacheObserver, MetricsCacheObserver>());
      return services;
    }

    /// <summary>
    /// Registers the logging-only cache observer.
    /// </summary>
    public static IServiceCollection AddLoggingCacheObserver(this IServiceCollection services)
    {
      services.TryAddSingleton<LoggingCacheObserver>();
      services.TryAddEnumerable(ServiceDescriptor.Singleton<ICacheObserver, LoggingCacheObserver>());
      return services;
    }

    /// <summary>
    /// Registers the hybrid observer (metrics + logging).
    /// </summary>
    public static IServiceCollection AddLoggingMetricsCacheObserver(this IServiceCollection services)
    {
      services.TryAddSingleton<LoggingMetricsObserver>();
      services.TryAddEnumerable(ServiceDescriptor.Singleton<ICacheObserver, LoggingMetricsObserver>());
      return services;
    }

    public static IServiceCollection AddExcelMetricsCacheObserver(this IServiceCollection services)
    {
      services.TryAddSingleton<ExcelCacheObserver>();
      services.TryAddEnumerable(ServiceDescriptor.Singleton<ICacheObserver, ExcelCacheObserver>());
      return services;
    }

    /// <summary>
    /// Registers a composite observer that calls all ICacheObservers.
    /// </summary>
    public static IServiceCollection AddCompositeCacheObserver(this IServiceCollection services)
    {
      services.AddSingleton<ICacheObserver>(sp =>
      {
        var observers = sp.GetServices<ICacheObserver>();
        return new CompositeCacheObserver(observers);
      });

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