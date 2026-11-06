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
    private const int BatchSize = 128;
    private const string HitKey = "benchmark:hit";
    private CacheProviderSession _session = null!;
    private string _payload = null!;

    [ParamsSource(nameof(Providers))]
    public CacheProviderKind Provider { get; set; }

    [Params(128, 4096, 65536)]
    public int PayloadSize { get; set; }

    public IEnumerable<CacheProviderKind> Providers => CacheProviderSession.GetProviders();

    private ICacheProvider Cache => _session.Cache;
    private ICacheWriter Writer => _session.Writer;

    [GlobalSetup]
    public async Task Setup()
    {
        _payload = new string('x', PayloadSize);
        ResetSession();
        await Writer.SetAsync(HitKey, _payload);
    }

    [IterationSetup(Target = nameof(MissBatch))]
    public void ResetMissIteration() => ResetSession();

    [IterationSetup(Target = nameof(WriteBatch))]
    public void ResetWriteIteration() => ResetSession();

    [GlobalCleanup]
    public void Cleanup() => _session?.Dispose();

    [Benchmark]
    public Task<CacheResult<string>> Hit()
        => Cache.GetOrSetAsync(HitKey, _ => Task.FromResult(_payload));

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public async Task MissBatch()
    {
        for (var index = 0; index < BatchSize; index++)
            await Cache.GetOrSetAsync($"benchmark:miss:{index}", _ => Task.FromResult(_payload));
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public async Task WriteBatch()
    {
        for (var index = 0; index < BatchSize; index++)
            await Writer.SetAsync($"benchmark:write:{index}", _payload);
    }

    [Benchmark]
    public Task Overwrite() => Writer.SetAsync(HitKey, _payload);

    private void ResetSession()
    {
        _session?.Dispose();
        _session = CacheProviderSession.Create(Provider);
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class CacheConcurrentHitBenchmarks
{
    private CacheProviderSession _session = null!;
    private string[] _keys = [];
    private string _payload = null!;

    [ParamsSource(nameof(Providers))]
    public CacheProviderKind Provider { get; set; }

    [Params(128, 4096, 65536)]
    public int PayloadSize { get; set; }

    [Params(1, 8, 32)]
    public int Concurrency { get; set; }

    public IEnumerable<CacheProviderKind> Providers => CacheProviderSession.GetProviders();

    [GlobalSetup]
    public async Task Setup()
    {
        _payload = new string('x', PayloadSize);
        _session = CacheProviderSession.Create(Provider);
        _keys = Enumerable.Range(0, Concurrency)
            .Select(index => $"benchmark:concurrent:{index}")
            .ToArray();

        foreach (var key in _keys)
            await _session.Writer.SetAsync(key, _payload);
    }

    [GlobalCleanup]
    public void Cleanup() => _session?.Dispose();

    [Benchmark(Description = "Concurrent cache-hit batch")]
    public async Task ConcurrentHitBatch()
    {
        var tasks = new Task<CacheResult<string>>[Concurrency];
        for (var index = 0; index < tasks.Length; index++)
        {
            var key = _keys[index];
            tasks[index] = _session.Cache.GetOrSetAsync(key, _ => Task.FromResult(_payload));
        }

        await Task.WhenAll(tasks);
    }
}

internal sealed class CacheProviderSession : IDisposable
{
    private readonly ServiceProvider _services;

    private CacheProviderSession(ServiceProvider services)
    {
        _services = services;
        Cache = services.GetRequiredService<ICacheProvider>();
        Writer = services.GetRequiredService<ICacheWriter>();
    }

    public ICacheProvider Cache { get; }
    public ICacheWriter Writer { get; }

    public static CacheProviderSession Create(CacheProviderKind provider)
    {
        var prefix = $"franz-benchmark-{Guid.NewGuid():N}:";
        var services = new ServiceCollection();

        switch (provider)
        {
            case CacheProviderKind.Memory:
                services.AddFranzMemoryCaching(options => ConfigureOptions(options, prefix));
                break;

            case CacheProviderKind.Distributed:
                services.AddDistributedMemoryCache();
                services.AddFranzCachingJsonTypeInfoResolver(BenchmarkJsonContext.Default);
                services.AddFranzDistributedCaching(options => ConfigureOptions(options, prefix));
                break;

            case CacheProviderKind.Hybrid:
                services.AddDistributedMemoryCache();
                services.AddFranzCachingJsonTypeInfoResolver(BenchmarkJsonContext.Default);
                services.AddFranzHybridCaching(options => ConfigureOptions(options, prefix));
                break;

            case CacheProviderKind.Redis:
                var connection = Environment.GetEnvironmentVariable("FRANZ_CACHE_BENCHMARK_REDIS")
                    ?? throw new InvalidOperationException("Set FRANZ_CACHE_BENCHMARK_REDIS to run Redis benchmarks.");
                services.AddFranzCachingJsonTypeInfoResolver(BenchmarkJsonContext.Default);
                services.AddFranzRedisCaching(options =>
                {
                    ConfigureOptions(options, prefix);
                    options.ConnectionString = connection;
                });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }

        return new CacheProviderSession(services.BuildServiceProvider(validateScopes: true));
    }

    public static IEnumerable<CacheProviderKind> GetProviders()
        => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FRANZ_CACHE_BENCHMARK_REDIS"))
            ? [CacheProviderKind.Memory, CacheProviderKind.Distributed, CacheProviderKind.Hybrid]
            : [CacheProviderKind.Memory, CacheProviderKind.Distributed, CacheProviderKind.Hybrid, CacheProviderKind.Redis];

    public void Dispose() => _services.Dispose();

    private static void ConfigureOptions(CacheOptions options, string prefix)
    {
        options.KeyPrefix = prefix;
        options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(1);
        options.LocalCacheHint = TimeSpan.FromSeconds(30);
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
