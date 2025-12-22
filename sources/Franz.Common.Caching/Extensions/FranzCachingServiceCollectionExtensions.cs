using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Franz.Common.Caching.Extensions
{
  public static class FranzCachingServiceCollectionExtensions
  {
    /// <summary>
    /// Adds Franz Caching with in-memory provider (default).
    /// </summary>
    public static IServiceCollection AddFranzMemoryCaching(
       this IServiceCollection services,
       Action<CacheEntryOptions>? configure = null)
    {
      services.AddMemoryCache();
      services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();
      if (configure != null) services.Configure(configure);
      return services;
    }

    /// <summary>
    /// Adds Franz Caching with a distributed provider (IDistributedCache).
    /// </summary>
    public static IServiceCollection AddFranzDistributedCaching<TDistributedCache>(
      this IServiceCollection services,
      Action<CacheEntryOptions>? configure = null)
      where TDistributedCache : class, IDistributedCache
    {
      services.AddSingleton<IDistributedCache, TDistributedCache>();
      services.TryAddSingleton<ICacheProvider, DistributedCacheProvider>();
      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();
      if (configure != null) services.Configure(configure);
      return services;
    }

    /// <summary>
    /// Adds Franz Caching with a Redis provider.
    /// </summary>
    public static IServiceCollection AddFranzRedisCaching(
    this IServiceCollection services,
    string connectionString,
    int database = 0,
    Action<CacheEntryOptions>? configure = null)
    {
      services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(
          new StackExchange.Redis.ConfigurationOptions
          {
            EndPoints = { connectionString },
            AbortOnConnectFail = false,
            DefaultDatabase = database
          }));

      services.AddSingleton<ICacheProvider>(sp =>
        new RedisCacheProvider(sp.GetRequiredService<IConnectionMultiplexer>()));

      services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
      services.TryAddSingleton<ISettingsCache, SettingsCache>();

      if (configure != null)
        services.Configure(configure);

      return services;
    }


    /// <summary>
    /// Convenience method: defaults to in-memory caching.
    /// </summary>
    public static IServiceCollection AddFranzCaching(
        this IServiceCollection services,
        Action<CacheEntryOptions>? configure = null)
        => services.AddFranzMemoryCaching(configure);

    public static IServiceCollection AddFranzMediatorCaching(this IServiceCollection services,
    Action<MediatorCachingOptions>? configure = null)
    {
      if (configure != null) services.Configure(configure);
      services.AddScoped(typeof(IPipeline<,>), typeof(CachingPipeline<,>));
      return services;
    }


  }
}
