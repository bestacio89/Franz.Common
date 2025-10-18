using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using FluentAssertions;

public class CachingPipelineTests
{
  private sealed class DummyCache : ICacheProvider
  {
    public int Hits, Misses;
    private readonly Dictionary<string, object> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(key, out var v) ? (T)v : default);
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    { _store[key] = value!; return Task.CompletedTask; }
    public Task RemoveAsync(string key, CancellationToken ct = default)
    { _store.Remove(key); return Task.CompletedTask; }
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_store.ContainsKey(key));
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
    var resp1 = await pipeline.Handle(new TestRequest("A"), () => Task.FromResult(new TestResponse("FromSource")));
    // Second call → HIT
    var resp2 = await pipeline.Handle(new TestRequest("A"), () => Task.FromResult(new TestResponse("Ignored")));

    resp1.Result.Should().Be("FromSource");
    resp2.Result.Should().Be("FromSource");
  }
}
