using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Caching.Benchmarks;

public enum CacheProviderKind { Memory, Distributed, Hybrid, Redis }

[MemoryDiagnoser]
[ShortRunJob]
public class CacheProviderBenchmarks
{
    private const string HitKey = "benchmark:hit";
    private ICacheProvider _provider = null!;
    private ICacheWriter _writer = null!;
    private ServiceProvider _services = null!;
    private string _payload = null!;
    private long _sequence;
    private string[] _concurrentKeys = [];

    [ParamsSource(nameof(Providers))]
    public CacheProviderKind Provider { get; set; }

    [Params(128, 4096, 65536)]
    public int PayloadSize { get; set; }

    [Params(1, 8, 32)]
    public int Concurrency { get; set; }

    public IEnumerable<CacheProviderKind> Providers =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FRANZ_CACHE_BENCHMARK_REDIS"))
            ? [CacheProviderKind.Memory, CacheProviderKind.Distributed, CacheProviderKind.Hybrid]
            : [CacheProviderKind.Memory, CacheProviderKind.Distributed, CacheProviderKind.Hybrid, CacheProviderKind.Redis];

    [GlobalSetup]
    public async Task Setup()
    {
        _payload = new string('x', PayloadSize);
        var services = new ServiceCollection();
        switch (Provider)
        {
            case CacheProviderKind.Memory:
                services.AddFranzMemoryCaching(ConfigureOptions);
                break;
            case CacheProviderKind.Distributed:
                services.AddDistributedMemoryCache();
                services.AddFranzCachingJsonTypeInfoResolver(BenchmarkJsonContext.Default);
                services.AddFranzDistributedCaching(ConfigureOptions);
                break;
            case CacheProviderKind.Hybrid:
                services.AddDistributedMemoryCache();
                services.AddFranzCachingJsonTypeInfoResolver(BenchmarkJsonContext.Default);
                services.AddFranzHybridCaching(ConfigureOptions);
                break;
            case CacheProviderKind.Redis:
                var connection = Environment.GetEnvironmentVariable("FRANZ_CACHE_BENCHMARK_REDIS")
                    ?? throw new InvalidOperationException("Set FRANZ_CACHE_BENCHMARK_REDIS to run Redis benchmarks.");
                services.AddFranzCachingJsonTypeInfoResolver(BenchmarkJsonContext.Default);
                services.AddFranzRedisCaching(options =>
                {
                    ConfigureOptions(options);
                    options.ConnectionString = connection;
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Provider));
        }

        _services = services.BuildServiceProvider(validateScopes: true);
        _provider = _services.GetRequiredService<ICacheProvider>();
        _writer = _services.GetRequiredService<ICacheWriter>();
        _concurrentKeys = Enumerable.Range(0, Concurrency).Select(i => $"benchmark:concurrent:{i}").ToArray();
        await _writer.SetAsync(HitKey, _payload);
        foreach (var key in _concurrentKeys)
            await _writer.SetAsync(key, _payload);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_services is not null)
            await _services.DisposeAsync();
    }

    [Benchmark]
    public Task<CacheResult<string>> Hit()
        => _provider.GetOrSetAsync(HitKey, _ => Task.FromResult(_payload));

    [Benchmark]
    public Task<CacheResult<string>> Miss()
        => _provider.GetOrSetAsync($"benchmark:miss:{Interlocked.Increment(ref _sequence)}",
            _ => Task.FromResult(_payload));

    [Benchmark]
    public Task Write()
        => _writer.SetAsync($"benchmark:write:{Interlocked.Increment(ref _sequence)}", _payload);

    [Benchmark]
    public Task Overwrite() => _writer.SetAsync(HitKey, _payload);

    [Benchmark]
    public async Task ConcurrentHits()
    {
        var tasks = new Task<CacheResult<string>>[Concurrency];
        for (var i = 0; i < tasks.Length; i++)
        {
            var key = _concurrentKeys[i];
            tasks[i] = _provider.GetOrSetAsync(key, _ => Task.FromResult(_payload));
        }
        await Task.WhenAll(tasks);
    }

    private static void ConfigureOptions(CacheOptions options)
    {
        options.KeyPrefix = "franz-benchmark:";
        options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(30);
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(10);
        options.LocalCacheHint = TimeSpan.FromMinutes(5);
        options.DefaultEstimatedSizeInBytes = 0;
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class CacheKeyGenerationBenchmarks
{
    private DefaultCacheKeyStrategy _strategy = null!;
    private BenchmarkRequest _request = null!;

    [Params(128, 4096, 65536)]
    public int PayloadSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            TypeInfoResolver = BenchmarkJsonContext.Default
        };
        _strategy = new DefaultCacheKeyStrategy(options);
        _request = new BenchmarkRequest("customer-42", new string('x', PayloadSize));
    }

    [Benchmark]
    public string BuildDeterministicSha256Key() => _strategy.BuildKey(_request);
}

public sealed record BenchmarkRequest(string CustomerId, string Payload);

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(BenchmarkRequest))]
internal partial class BenchmarkJsonContext : JsonSerializerContext
{
}
