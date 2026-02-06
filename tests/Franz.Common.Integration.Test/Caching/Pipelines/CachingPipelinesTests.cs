using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class CachingPipelineTests
{
  // ✅ DummyCache adapted to GetOrSetAsync with CacheResult<T>
  public sealed class DummyCache : ICacheProvider
  {
    private readonly ConcurrentDictionary<string, object> _store = new();
    private readonly ConcurrentDictionary<string, string[]> _tags = new();

    public int Hits, Misses;

    public Task<CacheResult<T>> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheOptions? options = null,
        CancellationToken ct = default)
    {
      if (string.IsNullOrWhiteSpace(key))
        throw new ArgumentException("Key cannot be null or empty", nameof(key));
      if (factory is null)
        throw new ArgumentNullException(nameof(factory));

      if (_store.TryGetValue(key, out var cached))
      {
        Hits++;
        return Task.FromResult(new CacheResult<T>((T)cached!, IsHit: true));
      }

      Misses++;

      return factory(ct).ContinueWith(t =>
      {
        var value = t.Result!;
        _store[key] = value;

        if (options?.Tags != null)
          _tags[key] = options.Tags;

        return new CacheResult<T>(value, IsHit: false);
      }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
      _store.TryRemove(key, out _);
      _tags.TryRemove(key, out _);
      return Task.CompletedTask;
    }

    public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
      foreach (var kvp in _tags)
      {
        if (kvp.Value != null && Array.Exists(kvp.Value, t => t == tag))
        {
          _store.TryRemove(kvp.Key, out _);
          _tags.TryRemove(kvp.Key, out _);
        }
      }
      return Task.CompletedTask;
    }
  }

  private record TestRequest(string Payload);
  private record TestResponse(string Result);

  [Fact]
  public async Task Should_Return_Cached_Value_On_Hit()
  {
    var cache = new DummyCache();
    var opts = Options.Create(new MediatorCachingOptions());
    var strategy = new Franz.Common.Caching.Estrategies.DefaultCacheKeyStrategy();
    var logger = NullLogger<CachingPipeline<TestRequest, TestResponse>>.Instance;

    var pipeline = new CachingPipeline<TestRequest, TestResponse>(cache, opts, strategy, logger);

    // First call → MISS
    var resp1Result = await pipeline.Handle(
        new TestRequest("A"),
        () => Task.FromResult(new TestResponse("FromSource"))
    );

    // Second call → HIT
    var resp2Result = await pipeline.Handle(
        new TestRequest("A"),
        () => Task.FromResult(new TestResponse("Ignored"))
    );

    // ✅ Assertions
    resp1Result.Should().Be("FromSource");
    resp2Result.Should().Be("FromSource");

    // ✅ Optional: verify hit/miss counters
    cache.Hits.Should().Be(1);
    cache.Misses.Should().Be(1);
  }

  [Fact]
  public async Task Should_Respect_Removal()
  {
    var cache = new DummyCache();
    var opts = Options.Create(new MediatorCachingOptions());
    var strategy = new Franz.Common.Caching.Estrategies.DefaultCacheKeyStrategy();
    var logger = NullLogger<CachingPipeline<TestRequest, TestResponse>>.Instance;

    var pipeline = new CachingPipeline<TestRequest, TestResponse>(cache, opts, strategy, logger);

    var key = new TestRequest("B");

    var first = await pipeline.Handle(key, () => Task.FromResult(new TestResponse("Original")));
    first.Should().Be("Original");

    await cache.RemoveAsync(strategy.GetKey(key));

    var second = await pipeline.Handle(key, () => Task.FromResult(new TestResponse("AfterRemove")));
    second.Should().Be("AfterRemove");
  }

  [Fact]
  public async Task Should_Throw_On_Null_Factory()
  {
    var cache = new DummyCache();
    var opts = Options.Create(new MediatorCachingOptions());
    var strategy = new Franz.Common.Caching.Estrategies.DefaultCacheKeyStrategy();
    var logger = NullLogger<CachingPipeline<TestRequest, TestResponse>>.Instance;

    var pipeline = new CachingPipeline<TestRequest, TestResponse>(cache, opts, strategy, logger);

    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        pipeline.Handle(new TestRequest("C"), null!));
  }
}
