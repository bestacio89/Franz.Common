using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Franz.Common.Caching;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Extensions;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using Testcontainers.Redis;

var providerName = Environment.GetEnvironmentVariable("FRANZ_CACHE_LOAD_PROVIDER") ?? "memory";
if (!Enum.TryParse<CacheProviderKind>(providerName, true, out var providerKind))
{
    Console.Error.WriteLine($"Unsupported provider '{providerName}'.");
    Environment.ExitCode = 2;
    return;
}

var profileName = Environment.GetEnvironmentVariable("FRANZ_CACHE_LOAD_PROFILE") ?? "ci";
if (!LoadProfile.TryCreate(profileName, out var profile))
{
    Console.Error.WriteLine($"Unsupported profile '{profileName}'. Expected ci, full, or stress.");
    Environment.ExitCode = 2;
    return;
}

RedisContainer? redis = null;
try
{
    string? redisConnectionString = null;
    if (providerKind != CacheProviderKind.Memory)
    {
        redis = new RedisBuilder("redis:7.2-alpine").WithCleanUp(true).Build();
        await redis.StartAsync();
        redisConnectionString = redis.GetConnectionString();
    }

    var prefix = $"franz-load-{Guid.NewGuid():N}:";
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddFranzCachingJsonTypeInfoResolver(CachingLoadJsonContext.Default);

    switch (providerKind)
    {
        case CacheProviderKind.Memory:
            services.AddFranzMemoryCaching(options => ConfigureOptions(options, prefix));
            break;
        case CacheProviderKind.Distributed:
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
            services.AddFranzDistributedCaching(options => ConfigureOptions(options, prefix));
            break;
        case CacheProviderKind.Hybrid:
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
            services.AddFranzHybridCaching(options => ConfigureOptions(options, prefix));
            break;
        case CacheProviderKind.Redis:
            services.AddFranzRedisCaching(options =>
            {
                ConfigureOptions(options, prefix);
                options.ConnectionString = redisConnectionString;
            });
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(providerKind));
    }

    await using var root = services.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true
    });

    var cache = root.GetRequiredService<ICacheProvider>();
    var writer = root.GetRequiredService<ICacheWriter>();
    var tenantIds = Enumerable.Range(0, profile.TenantCount).Select(CreateTenantId).ToArray();
    var strategies = CreateTenantKeyStrategies(tenantIds);
    var keys = BuildKeys(strategies, profile.KeysPerTenant);
    var payloads = BuildPayloads(tenantIds, profile.KeysPerTenant);

    await VerifyTenantIsolationAsync(cache, writer, strategies, tenantIds);
    await VerifyStampedeProtectionAsync(cache, strategies[0]);
    await VerifyExpirationAsync(cache, strategies[0]);

    var warmEntries = BuildWarmEntries(keys, payloads, profile.PrewarmPercentage);
    await Parallel.ForEachAsync(
        warmEntries,
        new ParallelOptions { MaxDegreeOfParallelism = profile.SetupParallelism },
        async (entry, cancellationToken) =>
            await writer.SetAsync(entry.Key, entry.Value, ct: cancellationToken));

    var counters = new LoadCounters();
    var processMetrics = new ProcessMetrics();
    var scenario = Scenario.Create("multi_tenant_cache_application_workload", async context =>
    {
        var tenantIndex = context.Random.Next(profile.TenantCount);
        var keyIndex = SelectSkewedKey(context.Random, profile.KeysPerTenant);
        var key = keys[tenantIndex][keyIndex];
        var payload = payloads[tenantIndex][keyIndex];
        var operation = context.Random.Next(100);

        if (operation < 80)
        {
            await Step.Run("cache_read", context, async () =>
            {
                try
                {
                    var result = await cache.GetOrSetAsync(
                        key,
                        _ =>
                        {
                            Interlocked.Increment(ref counters.FactoryExecutions);
                            return Task.FromResult(payload);
                        },
                        ct: context.ScenarioCancellationToken);

                    if (result.Value.TenantId != tenantIds[tenantIndex] || result.Value.KeyId != keyIndex)
                        throw new InvalidOperationException("Tenant/key isolation mismatch in cached payload.");

                    if (result.IsHit)
                        Interlocked.Increment(ref counters.Hits);
                    else
                        Interlocked.Increment(ref counters.Misses);

                    return Response.Ok();
                }
                catch (Exception exception)
                {
                    Interlocked.Increment(ref counters.Failures);
                    context.Logger.Error(exception, "Cache read workload operation failed.");
                    return Response.Fail();
                }
            });
        }
        else if (operation < 95)
        {
            await Step.Run("cache_write", context, async () =>
            {
                try
                {
                    await writer.SetAsync(key, payload, ct: context.ScenarioCancellationToken);
                    Interlocked.Increment(ref counters.Writes);
                    return Response.Ok();
                }
                catch (Exception exception)
                {
                    Interlocked.Increment(ref counters.Failures);
                    context.Logger.Error(exception, "Cache write workload operation failed.");
                    return Response.Fail();
                }
            });
        }
        else
        {
            var hotKey = keys[tenantIndex][0];
            var hotPayload = payloads[tenantIndex][0];
            await Step.Run("cache_overwrite", context, async () =>
            {
                try
                {
                    await writer.SetAsync(hotKey, hotPayload, ct: context.ScenarioCancellationToken);
                    Interlocked.Increment(ref counters.Overwrites);
                    return Response.Ok();
                }
                catch (Exception exception)
                {
                    Interlocked.Increment(ref counters.Failures);
                    context.Logger.Error(exception, "Cache overwrite workload operation failed.");
                    return Response.Fail();
                }
            });
        }

        return Response.Ok();
    })
    .WithWarmUpDuration(TimeSpan.FromSeconds(profile.WarmUpSeconds))
    .WithLoadSimulations(
        Simulation.RampingConstant(copies: profile.VirtualUsers, during: TimeSpan.FromSeconds(profile.RampUpSeconds)),
        Simulation.KeepConstant(copies: profile.VirtualUsers, during: TimeSpan.FromSeconds(profile.SustainedSeconds)),
        Simulation.RampingConstant(copies: 0, during: TimeSpan.FromSeconds(profile.RampDownSeconds)))
    .WithMaxFailCount(profile.MaxFailures)
    .WithInit(_ =>
    {
        processMetrics.Start();
        return Task.CompletedTask;
    })
    .WithClean(_ =>
    {
        processMetrics.Stop();
        return Task.CompletedTask;
    });

    var reportFolder = Path.Combine("reports", providerKind.ToString().ToLowerInvariant());
    Directory.CreateDirectory(reportFolder);

    Console.WriteLine(
        $"Starting {providerKind} profile '{profile.Name}': {profile.TenantCount:N0} tenants, " +
        $"{profile.KeysPerTenant:N0} keys/tenant, {profile.VirtualUsers:N0} virtual users, 80/15/5 workload.");

    NBomberRunner
        .RegisterScenarios(scenario)
        .WithTestSuite("Franz.Common.Caching")
        .WithTestName($"{providerKind}-{profile.Name}")
        .WithReportFolder(reportFolder)
        .WithReportFileName($"caching-{providerKind.ToString().ToLowerInvariant()}-{profile.Name}")
        .WithReportFormats(ReportFormat.Html, ReportFormat.Md, ReportFormat.Csv)
        .WithScenarioCompletionTimeout(TimeSpan.FromMinutes(2))
        .Run();

    var summary = new LoadTestSummary(
        Provider: providerKind.ToString(),
        Profile: profile.Name,
        Commit: Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
        Runtime: RuntimeInformation.FrameworkDescription,
        OperatingSystem: RuntimeInformation.OSDescription,
        ProcessorCount: Environment.ProcessorCount,
        TenantCount: profile.TenantCount,
        KeysPerTenant: profile.KeysPerTenant,
        PrewarmedKeys: warmEntries.Count,
        VirtualUsers: profile.VirtualUsers,
        SustainedSeconds: profile.SustainedSeconds,
        Hits: Interlocked.Read(ref counters.Hits),
        Misses: Interlocked.Read(ref counters.Misses),
        Writes: Interlocked.Read(ref counters.Writes),
        Overwrites: Interlocked.Read(ref counters.Overwrites),
        FactoryExecutions: Interlocked.Read(ref counters.FactoryExecutions),
        Failures: Interlocked.Read(ref counters.Failures),
        HitRatio: CalculateHitRatio(counters),
        ProcessCpuMilliseconds: processMetrics.CpuMilliseconds,
        AllocatedBytes: processMetrics.AllocatedBytes,
        Gen0Collections: processMetrics.Gen0Collections,
        Gen1Collections: processMetrics.Gen1Collections,
        Gen2Collections: processMetrics.Gen2Collections);

    await File.WriteAllTextAsync(
        Path.Combine(reportFolder, "load-summary.json"),
        JsonSerializer.Serialize(summary, CachingLoadJsonContext.Default.LoadTestSummary));

    if (summary.Failures > 0)
    {
        Console.Error.WriteLine($"Production workload recorded {summary.Failures:N0} failed operations.");
        Environment.ExitCode = 1;
    }
    else
    {
        Console.WriteLine(
            $"Workload completed: {summary.HitRatio:P2} read hit ratio, " +
            $"{summary.Hits + summary.Misses:N0} reads, {summary.Writes + summary.Overwrites:N0} writes.");
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    Environment.ExitCode = 1;
}
finally
{
    if (redis is not null)
        await redis.DisposeAsync();
}

static void ConfigureOptions(CacheOptions options, string prefix)
{
    options.KeyPrefix = prefix;
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(60);
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
    options.LocalCacheHint = TimeSpan.FromMinutes(5);
    options.DefaultEstimatedSizeInBytes = 8 * 1024;
}

static Guid CreateTenantId(int index)
    => Guid.Parse($"00000000-0000-0000-0000-{index + 1:x12}");

static TenantScopedCacheKeyStrategy[] CreateTenantKeyStrategies(IReadOnlyList<Guid> tenantIds)
{
    var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
    {
        TypeInfoResolver = CachingLoadJsonContext.Default
    };
    var inner = new DefaultCacheKeyStrategy(serializerOptions);

    return tenantIds
        .Select(tenantId => new TenantScopedCacheKeyStrategy(
            inner,
            new FixedTenantExecutionContextAccessor(CreateTenantContext(tenantId))))
        .ToArray();
}

static TenantExecutionContext CreateTenantContext(Guid tenantId)
    => new(
        new TenantInfo(tenantId, $"tenant-{tenantId:N}"),
        domainId: null,
        TenantResolutionSource.JwtClaim,
        DateTimeOffset.UtcNow);

static string[][] BuildKeys(IReadOnlyList<TenantScopedCacheKeyStrategy> strategies, int keysPerTenant)
{
    var keys = new string[strategies.Count][];
    for (var tenantIndex = 0; tenantIndex < strategies.Count; tenantIndex++)
    {
        keys[tenantIndex] = new string[keysPerTenant];
        for (var keyIndex = 0; keyIndex < keysPerTenant; keyIndex++)
            keys[tenantIndex][keyIndex] = strategies[tenantIndex].BuildKey(new LoadRequest(keyIndex));
    }

    return keys;
}

static LoadPayload[][] BuildPayloads(IReadOnlyList<Guid> tenantIds, int keysPerTenant)
{
    var payloadBodies = new[]
    {
        new string('x', 128),
        new string('x', 4 * 1024),
        new string('x', 64 * 1024)
    };

    var payloads = new LoadPayload[tenantIds.Count][];
    for (var tenantIndex = 0; tenantIndex < tenantIds.Count; tenantIndex++)
    {
        payloads[tenantIndex] = new LoadPayload[keysPerTenant];
        for (var keyIndex = 0; keyIndex < keysPerTenant; keyIndex++)
        {
            var bodyIndex = keyIndex % 10 < 5 ? 0 : keyIndex % 10 < 9 ? 1 : 2;
            payloads[tenantIndex][keyIndex] = new LoadPayload(tenantIds[tenantIndex], keyIndex, payloadBodies[bodyIndex]);
        }
    }

    return payloads;
}

static List<(string Key, LoadPayload Value)> BuildWarmEntries(
    IReadOnlyList<string[]> keys,
    IReadOnlyList<LoadPayload[]> payloads,
    int prewarmPercentage)
{
    var entries = new List<(string Key, LoadPayload Value)>();
    for (var tenantIndex = 0; tenantIndex < keys.Count; tenantIndex++)
    {
        var warmCount = (int)Math.Ceiling(keys[tenantIndex].Length * prewarmPercentage / 100d);
        for (var keyIndex = 0; keyIndex < warmCount; keyIndex++)
            entries.Add((keys[tenantIndex][keyIndex], payloads[tenantIndex][keyIndex]));
    }

    return entries;
}

static int SelectSkewedKey(Random random, int keyCount)
{
    // Power-law-like skew gives a realistic hot working set without external traces.
    var sample = Math.Pow(random.NextDouble(), 4);
    return Math.Min(keyCount - 1, (int)(sample * keyCount));
}

static async Task VerifyTenantIsolationAsync(
    ICacheProvider cache,
    ICacheWriter writer,
    IReadOnlyList<TenantScopedCacheKeyStrategy> strategies,
    IReadOnlyList<Guid> tenantIds)
{
    var request = new LoadRequest(-1);
    var keyA = strategies[0].BuildKey(request);
    var keyB = strategies[1].BuildKey(request);
    if (StringComparer.Ordinal.Equals(keyA, keyB))
        throw new InvalidOperationException("Different tenants produced the same scoped cache key.");

    var valueA = new LoadPayload(tenantIds[0], -1, "tenant-a");
    var valueB = new LoadPayload(tenantIds[1], -1, "tenant-b");
    await writer.SetAsync(keyA, valueA);
    await writer.SetAsync(keyB, valueB);

    var resultA = await cache.GetOrSetAsync(keyA, _ => Task.FromResult(valueA));
    var resultB = await cache.GetOrSetAsync(keyB, _ => Task.FromResult(valueB));
    if (resultA.Value.TenantId != tenantIds[0] || resultB.Value.TenantId != tenantIds[1])
        throw new InvalidOperationException("Cache returned a value belonging to a different tenant.");
}

static async Task VerifyStampedeProtectionAsync(ICacheProvider cache, TenantScopedCacheKeyStrategy strategy)
{
    const int callers = 64;
    var key = strategy.BuildKey(new LoadRequest(-2));
    var factoryCalls = 0;
    var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseFactory = new TaskCompletionSource<LoadPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
    var expected = new LoadPayload(Guid.Empty, -2, "single-flight");

    var operations = Enumerable.Range(0, callers)
        .Select(async _ =>
        {
            await start.Task;
            return await cache.GetOrSetAsync(
                key,
                async cancellationToken =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    return await releaseFactory.Task.WaitAsync(cancellationToken);
                });
        })
        .ToArray();

    start.SetResult();
    await Task.Delay(TimeSpan.FromMilliseconds(100));
    releaseFactory.SetResult(expected);
    var results = await Task.WhenAll(operations);

    if (factoryCalls != 1 || results.Any(result => result.Value != expected))
        throw new InvalidOperationException($"Stampede protection expected one factory execution, observed {factoryCalls}.");
}

static async Task VerifyExpirationAsync(ICacheProvider cache, TenantScopedCacheKeyStrategy strategy)
{
    var key = strategy.BuildKey(new LoadRequest(-3));
    var options = new CacheOptions
    {
        DefaultAbsoluteExpiration = TimeSpan.FromMilliseconds(250),
        DefaultSlidingExpiration = TimeSpan.Zero,
        LocalCacheHint = TimeSpan.Zero
    };

    await cache.GetOrSetAsync(key, _ => Task.FromResult("initial"), options);
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    var expired = await cache.GetOrSetAsync(key, _ => Task.FromResult("refreshed"), options);
    if (expired.IsHit || expired.Value != "refreshed")
        throw new InvalidOperationException("Absolute expiration validation failed.");
}

static double CalculateHitRatio(LoadCounters counters)
{
    var hits = Interlocked.Read(ref counters.Hits);
    var misses = Interlocked.Read(ref counters.Misses);
    return hits + misses == 0 ? 0 : (double)hits / (hits + misses);
}

internal enum CacheProviderKind { Memory, Distributed, Hybrid, Redis }

internal sealed record LoadProfile(
    string Name, int TenantCount, int KeysPerTenant, int PrewarmPercentage,
    int VirtualUsers, int WarmUpSeconds, int RampUpSeconds, int SustainedSeconds,
    int RampDownSeconds, int SetupParallelism, int MaxFailures)
{
    public static bool TryCreate(string name, out LoadProfile profile)
    {
        profile = name.ToLowerInvariant() switch
        {
            "ci" => new("ci", 32, 250, 30, 24, 5, 5, 45, 5, 24, 10),
            "full" => new("full", 100, 1_000, 30, 100, 30, 30, 600, 30, 48, 25),
            "stress" => new("stress", 100, 1_000, 30, 256, 30, 60, 1_200, 60, 64, 50),
            _ => null!
        };
        return profile is not null;
    }
}

internal sealed class FixedTenantExecutionContextAccessor(TenantExecutionContext context)
    : ITenantExecutionContextAccessor
{
    public TenantExecutionContext? Current { get; private set; } = context;
    public void Initialize(TenantExecutionContext value) => Current = value;
    public void Clear() => Current = null;
}

internal sealed class LoadCounters
{
    public long Hits;
    public long Misses;
    public long Writes;
    public long Overwrites;
    public long FactoryExecutions;
    public long Failures;
}

internal sealed class ProcessMetrics
{
    private TimeSpan _cpuAtStart;
    private long _allocatedAtStart;
    private int _gen0AtStart;
    private int _gen1AtStart;
    private int _gen2AtStart;

    public double CpuMilliseconds { get; private set; }
    public long AllocatedBytes { get; private set; }
    public int Gen0Collections { get; private set; }
    public int Gen1Collections { get; private set; }
    public int Gen2Collections { get; private set; }

    public void Start()
    {
        using var process = Process.GetCurrentProcess();
        _cpuAtStart = process.TotalProcessorTime;
        _allocatedAtStart = GC.GetTotalAllocatedBytes(precise: true);
        _gen0AtStart = GC.CollectionCount(0);
        _gen1AtStart = GC.CollectionCount(1);
        _gen2AtStart = GC.CollectionCount(2);
    }

    public void Stop()
    {
        using var process = Process.GetCurrentProcess();
        CpuMilliseconds = (process.TotalProcessorTime - _cpuAtStart).TotalMilliseconds;
        AllocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - _allocatedAtStart;
        Gen0Collections = GC.CollectionCount(0) - _gen0AtStart;
        Gen1Collections = GC.CollectionCount(1) - _gen1AtStart;
        Gen2Collections = GC.CollectionCount(2) - _gen2AtStart;
    }
}

internal sealed record LoadTestSummary(
    string Provider, string Profile, string Commit, string Runtime, string OperatingSystem,
    int ProcessorCount, int TenantCount, int KeysPerTenant, int PrewarmedKeys, int VirtualUsers,
    int SustainedSeconds, long Hits, long Misses, long Writes, long Overwrites,
    long FactoryExecutions, long Failures, double HitRatio, double ProcessCpuMilliseconds,
    long AllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections);

internal sealed record LoadRequest(int KeyId);
internal sealed record LoadPayload(Guid TenantId, int KeyId, string Content);

[JsonSerializable(typeof(LoadRequest))]
[JsonSerializable(typeof(LoadPayload))]
[JsonSerializable(typeof(LoadTestSummary))]
internal partial class CachingLoadJsonContext : JsonSerializerContext { }
