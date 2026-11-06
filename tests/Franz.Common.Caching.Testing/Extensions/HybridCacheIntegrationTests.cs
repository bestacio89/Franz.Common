using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Caching.Tests.Extensions;

public sealed class HybridCacheIntegrationTests
{
  private sealed record CachePayload(string Value);

  [Fact]
  public async Task HybridCache_Should_ReadSerializedValuesFromSharedL2()
  {
    var sharedDistributedCache = new SharedDistributedCache();
    var firstServices = CreateServices(sharedDistributedCache);
    using var firstProvider = firstServices.BuildServiceProvider();

    var secondServices = CreateServices(sharedDistributedCache);
    using var secondProvider = secondServices.BuildServiceProvider();

    var firstCache = firstProvider.GetRequiredService<ICacheProvider>();
    var secondCache = secondProvider.GetRequiredService<ICacheProvider>();
    await ((ICacheWriter)firstCache).SetAsync("customer:42", new CachePayload("from-l2"));

    var keyPrefix = firstProvider.GetRequiredService<IOptions<CacheOptions>>().Value.KeyPrefix;
    var serializedEntry = await sharedDistributedCache.GetAsync($"{keyPrefix}customer:42");
    serializedEntry.Should().NotBeNull("the first HybridCache instance must persist the serialized payload to L2");

    var factoryCalls = 0;
    var result = await secondCache.GetOrSetAsync("customer:42", _ =>
    {
      factoryCalls++;
      return Task.FromResult(new CachePayload("factory"));
    });

    result.IsHit.Should().BeTrue();
    result.Value.Should().Be(new CachePayload("from-l2"));
    factoryCalls.Should().Be(0);
  }

  [Fact]
  public async Task HybridCache_Should_Not_KeepL1EntriesBeyondAbsoluteExpiration()
  {
    var services = CreateServices(configure: options =>
    {
      options.DefaultAbsoluteExpiration = TimeSpan.FromMilliseconds(250);
      options.DefaultSlidingExpiration = TimeSpan.Zero;
      options.LocalCacheHint = TimeSpan.FromSeconds(10);
    });
    using var serviceProvider = services.BuildServiceProvider();
    var cache = serviceProvider.GetRequiredService<ICacheProvider>();

    var first = await cache.GetOrSetAsync("absolute-bound", _ => Task.FromResult(new CachePayload("first")));
    first.IsHit.Should().BeFalse();

    await Task.Delay(500);
    var second = await cache.GetOrSetAsync("absolute-bound", _ => Task.FromResult(new CachePayload("second")));

    second.IsHit.Should().BeFalse();
    second.Value.Should().Be(new CachePayload("second"));
  }

  [Fact]
  public async Task HybridCache_Should_TreatZeroLocalCacheHintAsNoExpirationOverride()
  {
    var services = CreateServices(configure: options =>
    {
      options.DefaultAbsoluteExpiration = TimeSpan.FromSeconds(5);
      options.DefaultSlidingExpiration = TimeSpan.Zero;
      options.LocalCacheHint = TimeSpan.Zero;
    });
    using var serviceProvider = services.BuildServiceProvider();
    var cache = serviceProvider.GetRequiredService<ICacheProvider>();

    await cache.GetOrSetAsync("zero-local-hint", _ => Task.FromResult(new CachePayload("cached")));
    await Task.Delay(100);

    var result = await cache.GetOrSetAsync("zero-local-hint", _ => Task.FromResult(new CachePayload("factory")));

    result.IsHit.Should().BeTrue();
    result.Value.Should().Be(new CachePayload("cached"));
  }

  [Fact]
  public async Task HybridCache_Should_KeepL2FailuresBestEffort()
  {
    var services = CreateServices(new ThrowingDistributedCache());
    using var serviceProvider = services.BuildServiceProvider();
    var cache = serviceProvider.GetRequiredService<ICacheProvider>();

    var result = await cache.GetOrSetAsync("l2-unavailable", _ => Task.FromResult(new CachePayload("factory")));

    result.IsHit.Should().BeFalse();
    result.Value.Should().Be(new CachePayload("factory"));

    var writer = (ICacheWriter)cache;
    var act = () => writer.SetAsync("l2-unavailable-write", new CachePayload("value"));
    await act.Should().NotThrowAsync();
  }

  private static ServiceCollection CreateServices(
      IDistributedCache? distributedCache = null,
      Action<CacheOptions>? configure = null)
  {
    var services = new ServiceCollection();
    services.AddLogging();

    if (distributedCache is not null)
    {
      services.AddSingleton<IDistributedCache>(distributedCache);
    }

    services.AddFranzCachingJsonTypeInfoResolver(TestJsonSerializerOptions.Create().TypeInfoResolver!);
    services.AddFranzHybridCaching(configure);
    return services;
  }

  // HybridCache intentionally disables MemoryDistributedCache as L2 when paired
  // with MemoryCache L1. The wrapper gives the shared test backend a distinct
  // concrete type so the test exercises the actual L2 serialization path.
  private sealed class SharedDistributedCache : IDistributedCache
  {
    private readonly MemoryDistributedCache _inner = new(
        Options.Create(new MemoryDistributedCacheOptions()));

    public byte[]? Get(string key) => _inner.Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => _inner.GetAsync(key, token);

    public void Refresh(string key) => _inner.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default)
        => _inner.RefreshAsync(key, token);

    public void Remove(string key) => _inner.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
        => _inner.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => _inner.Set(key, value, options);

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
        => _inner.SetAsync(key, value, options, token);
  }

  private sealed class ThrowingDistributedCache : IDistributedCache
  {
    private static InvalidOperationException CreateException()
        => new("HybridCache L2 is unavailable.");

    public byte[]? Get(string key) => throw CreateException();

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => Task.FromException<byte[]?>(CreateException());

    public void Refresh(string key) => throw CreateException();

    public Task RefreshAsync(string key, CancellationToken token = default)
        => Task.FromException(CreateException());

    public void Remove(string key) => throw CreateException();

    public Task RemoveAsync(string key, CancellationToken token = default)
        => Task.FromException(CreateException());

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => throw CreateException();

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
        => Task.FromException(CreateException());
  }
}
