using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.MultiTenancy;
using Franz.Common.Caching.MultiTenancy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Caching.Extensions;

public static class FranzCachingServiceCollectionExtensions
{
  private const string CachingSection = CacheOptions.ConfigurationSectionName;


  /// <summary>
  /// Configures the core Caching Options with Fail-Fast Validation and IOptionsMonitor support.
  /// </summary>
  public static IServiceCollection AddFranzCachingOptions(this IServiceCollection services, IConfiguration configuration)
  {
    AddValidatedCacheOptions(services)
        .Bind(configuration.GetSection(CachingSection));

    return services;
  }

  /// <summary>
  /// Adds source-generated JSON metadata to cache serialization. Register
  /// the application's JsonSerializerContext so cache keys remain compatible
  /// with trimming and Native AOT.
  /// </summary>
  public static IServiceCollection AddFranzCachingJsonTypeInfoResolver(
      this IServiceCollection services,
      IJsonTypeInfoResolver resolver)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(resolver);

    services.Configure<JsonSerializerOptions>(options =>
    {
      options.TypeInfoResolver = options.TypeInfoResolver is null
          ? resolver
          : JsonTypeInfoResolver.Combine(resolver, options.TypeInfoResolver);
    });

    return services;
  }

  #region Cache Providers

  public static IServiceCollection AddFranzMemoryCaching(
      this IServiceCollection services,
      Action<CacheOptions>? configure = null)
  {
    services.AddMemoryCache();
    AddValidatedCacheOptions(services);

    if (configure != null) services.Configure(configure);

    services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
    return services.AddFranzCachingCore();
  }

  /// <summary>
  /// Registers the .NET HybridCache provider. Register an IDistributedCache
  /// implementation separately when a distributed L2 store is required.
  /// </summary>
  public static IServiceCollection AddFranzHybridCaching(
      this IServiceCollection services,
      Action<CacheOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);
    services.AddHybridCache().AddSerializerFactory<JsonTypeInfoHybridCacheSerializerFactory>();
    AddValidatedCacheOptions(services);

    if (configure is not null)
    {
      services.Configure(configure);
    }

    services.TryAddSingleton<ICacheProvider, HybridCacheProvider>();
    return services.AddFranzCachingCore();
  }

  /// <summary>
  /// Registers HybridCache and binds Franz cache options from configuration.
  /// An IDistributedCache implementation may be registered separately for L2.
  /// </summary>
  public static IServiceCollection AddFranzHybridCaching(
      this IServiceCollection services,
      IConfiguration configuration,
      Action<CacheOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    services.AddHybridCache().AddSerializerFactory<JsonTypeInfoHybridCacheSerializerFactory>();
    services.AddFranzCachingOptions(configuration);

    if (configure is not null)
    {
      services.Configure(configure);
    }

    services.TryAddSingleton<ICacheProvider, HybridCacheProvider>();
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
    AddValidatedCacheOptions(services);
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
    AddValidatedCacheOptions(services)
        .Bind(configuration.GetSection(CachingSection));

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

      // StackExchange.Redis exposes synchronous multiplexer creation through
      // this DI factory. Keep the initial connection timeout bounded and allow
      // the multiplexer to reconnect if Redis is temporarily unavailable.
      var redisOptions = ConfigurationOptions.Parse(connectionString);
      redisOptions.AbortOnConnectFail = false;
      redisOptions.ConnectTimeout = 5000;
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
        .Bind(configuration.GetSection(MediatorCachingOptions.SectionName))
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

  private static OptionsBuilder<CacheOptions> AddValidatedCacheOptions(IServiceCollection services)
  {
    return services.AddOptions<CacheOptions>()
        .ValidateDataAnnotations()
        .Validate(
            options => options.DefaultAbsoluteExpiration >= TimeSpan.Zero &&
                       options.DefaultSlidingExpiration >= TimeSpan.Zero &&
                       (!options.LocalCacheHint.HasValue || options.LocalCacheHint.Value >= TimeSpan.Zero),
            "Cache expirations and local-cache hints must be zero (disabled) or positive.")
        .ValidateOnStart();
  }

  private static IServiceCollection AddFranzCachingCore(this IServiceCollection services)
  {
    services.AddOptions<JsonSerializerOptions>();
    services.TryAddSingleton<ICacheKeyStrategy, DefaultCacheKeyStrategy>();
    services.TryAddSingleton<ISettingsCache, SettingsCache>();
    services.TryAddTransient<ICacheWriter>(sp =>
        sp.GetRequiredService<ICacheProvider>() as ICacheWriter
        ?? throw new InvalidOperationException("The configured cache provider does not implement ICacheWriter."));
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
    AddValidatedCacheOptions(services);

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

  /// <summary>
  /// Replaces the global cache-key strategy with a scoped strategy that prefixes every key
  /// with the validated tenant ID. Call after the cache provider registration.
  /// </summary>
  public static IServiceCollection AddTenantScopedCacheKeys(this IServiceCollection services)
  {
    services.TryAddSingleton<DefaultCacheKeyStrategy>();
    services.Replace(ServiceDescriptor.Scoped<ICacheKeyStrategy, TenantScopedCacheKeyStrategy>());
    return services;
  }

  /// <summary>
  /// Decorates the registered tenant store with time-bounded process-local caching.
  /// The application must invalidate the cache after every successful tenant registry write.
  /// </summary>
  public static IServiceCollection AddTenantStoreCaching(
      this IServiceCollection services,
      Action<TenantStoreCacheOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);

    if (services.Any(descriptor => descriptor.ServiceType == typeof(TenantStoreCacheRegistration)))
      return services;

    services.AddMemoryCache();
    services.AddOptions<TenantStoreCacheOptions>()
        .Configure(configure ?? (_ => { }))
        .Validate(
            options => options.AbsoluteExpiration > TimeSpan.Zero,
            "Tenant-store cache expiration must be greater than zero.")
        .ValidateOnStart();

    services.TryAddSingleton<TenantStoreCacheState>();
    services.TryAddSingleton<ITenantStoreCacheInvalidator, TenantStoreCacheInvalidator>();
    services.Decorate<ITenantStore, CachedTenantStore>();
    services.AddSingleton<TenantStoreCacheRegistration>();
    return services;
  }

  private sealed class TenantStoreCacheRegistration { }

}
