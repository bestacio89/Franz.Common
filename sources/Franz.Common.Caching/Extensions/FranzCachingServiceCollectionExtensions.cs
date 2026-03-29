using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Franz.Common.AzureCosmosDB.Extensions;

public static class FranzCachingServiceCollectionExtensions
{
  private const string CachingSection = "Franz:Caching";
  private const string MediatorCachingSection = "Franz:Mediator:Caching";

  /// <summary>
  /// Configures the core Caching Options with Fail-Fast Validation and IOptionsMonitor support.
  /// </summary>
  public static IServiceCollection AddFranzCachingOptions(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddOptions<CacheOptions>()
        .Bind(configuration.GetSection(CachingSection))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    return services;
  }

  #region Cache Providers

  public static IServiceCollection AddFranzMemoryCaching(
      this IServiceCollection services,
      Action<CacheOptions>? configure = null)
  {
    services.AddMemoryCache();

    if (configure != null) services.Configure(configure);

    services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
    return services.AddFranzCachingCore();
  }

  /// <summary>
  /// Overload for Fixtures/Tests. Allows configuration via Lambda without IConfiguration.
  /// Fixes: "Cannot convert lambda expression to type 'IConfiguration'"
  /// </summary>
  public static IServiceCollection AddFranzRedisCaching(
      this IServiceCollection services,
      Action<CacheOptions> configure)
  {
    services.AddOptions<CacheOptions>();
    services.Configure(configure);

    return services.RegisterRedisInternal();
  }

  /// <summary>
  /// Overload for Production. Binds to IConfiguration and allows optional Lambda overrides.
  /// </summary>
  public static IServiceCollection AddFranzRedisCaching(
      this IServiceCollection services,
      IConfiguration configuration,
      Action<CacheOptions>? configure = null)
  {
    // 1. Bind from appsettings.json
    services.AddOptions<CacheOptions>()
        .Bind(configuration.GetSection(CachingSection))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // 2. Apply optional code-based overrides
    if (configure != null) services.Configure(configure);

    return services.RegisterRedisInternal(configuration);
  }

  private static IServiceCollection RegisterRedisInternal(this IServiceCollection services, IConfiguration? configuration = null)
  {
    services.TryAddSingleton<IConnectionMultiplexer>(sp =>
    {
      var options = sp.GetRequiredService<IOptions<CacheOptions>>().Value;

      var connectionString = options.ConnectionString
          ?? configuration?.GetSection($"{CachingSection}:Redis:ConnectionString").Value
          ?? throw new InvalidOperationException("Redis ConnectionString is missing.");

      // FIX: Use ConfigurationOptions to enforce better defaults for internal tools
      var redisOptions = ConfigurationOptions.Parse(connectionString);

      // Ensure that we don't stall the thread pool during heavy load/reconnects
      redisOptions.AbortOnConnectFail = false;
      redisOptions.ConnectTimeout = 5000;

      // Still eager, but AbortOnConnectFail = false allows the object to be created 
      // even if the server is offline. It will reconnect in the background.
      return ConnectionMultiplexer.Connect(redisOptions);
    });

    services.TryAddSingleton<ICacheProvider, RedisCacheProvider>();
    return services.AddFranzCachingCore();
  }
  #endregion

  #region Mediator Pipeline

  public static IServiceCollection AddFranzMediatorCaching(
      this IServiceCollection services,
      IConfiguration configuration,
      Action<MediatorCachingOptions>? configure = null)
  {
    services.AddOptions<MediatorCachingOptions>()
        .BindConfiguration(MediatorCachingOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    if (configure != null) services.Configure(configure);

    // MediatR Pipeline Behavior registration
    services.AddScoped(typeof(IPipeline<,>), typeof(CachingPipeline<,>));

    return services;
  }

  #endregion

  #region Observability (Using Scrutor)

  /// <summary>
  /// Modernized Decorator Pattern using Scrutor. 
  /// This wraps any registered ICacheProvider with the Observable logic.
  /// </summary>
  public static IServiceCollection AddObservableCaching(this IServiceCollection services)
  {
    // Scrutor: Decorate the registered ICacheProvider without breaking DI descriptors.
    services.Decorate<ICacheProvider>((inner, sp) =>
    {
      var observers = sp.GetServices<ICacheObserver>();
      return new ObservableCacheProvider(inner, observers);
    });

    return services;
  }

  public static IServiceCollection AddCacheObserver<T>(this IServiceCollection services)
      where T : class, ICacheObserver
  {
    services.TryAddEnumerable(ServiceDescriptor.Singleton<ICacheObserver, T>());
    return services;
  }

  #endregion

  private static IServiceCollection AddFranzCachingCore(this IServiceCollection services)
  {
    services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
    services.TryAddSingleton<ISettingsCache, SettingsCache>();
    return services;
  }

  /// <summary>
  /// Adds a Distributed Cache Provider that wraps any registered IDistributedCache.
  /// </summary>
  public static IServiceCollection AddFranzDistributedCaching(
      this IServiceCollection services,
      Action<CacheOptions>? configure = null)
  {
    // 1. Ensure the Options record is available in the DI container
    services.AddOptions<CacheOptions>();

    // 2. Apply code-based overrides if provided
    if (configure != null) services.Configure(configure);

    // 3. Register our Adapter/Provider
    services.TryAddSingleton<ICacheProvider, DistributedCacheProvider>();

    return services.AddFranzCachingCore();
  }

  /// <summary>
  /// Adds a Distributed Cache Provider bound to a specific Configuration section.
  /// </summary>
  public static IServiceCollection AddFranzDistributedCaching(
      this IServiceCollection services,
      IConfiguration configuration,
      Action<CacheOptions>? configure = null)
  {
    // Bind global options from JSON
    services.AddFranzCachingOptions(configuration);

    if (configure != null) services.Configure(configure);

    services.TryAddSingleton<ICacheProvider, DistributedCacheProvider>();

    return services.AddFranzCachingCore();
  }
}