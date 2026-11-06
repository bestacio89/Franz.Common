using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Franz.Common.Caching;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Extensions;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

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

    Console.WriteLine(
        $"Starting {providerKind} profile '{profile.Name}': {profile.TenantCount:N0} tenants, " +
        $"{profile.KeysPerTenant:N0} keys/tenant, {profile.VirtualUsers:N0} virtual users, " +
        $"80/15/5 read/write/overwrite, {profile.ThinkTimeMilliseconds} ms base think time.");

    var warmupProfile = profile with
    {
        VirtualUsers = Math.Max(1, profile.VirtualUsers / 2),
        RampUpSeconds = 0,
        SustainedSeconds = profile.WarmUpSeconds,
        RampDownSeconds = 0
    };
    var warmupCounters = new LoadCounters();
    await RunWorkloadAsync(cache, writer, tenantIds, keys, payloads, warmupProfile, warmupCounters);
    if (warmupCounters.Failures > 0)
        throw new InvalidOperationException($"Warm-up phase recorded {warmupCounters.Failures:N0} failed operations.");

    var counters = new LoadCounters();
    var processMetrics = new ProcessMetrics();
    processMetrics.Start();
    var measuredDuration = await RunWorkloadAsync(cache, writer, tenantIds, keys, payloads, profile, counters);
    processMetrics.Stop();

    var operationReports = new[]
    {
        counters.Read.CreateReport("Read", measuredDuration),
        counters.Write.CreateReport("Write", measuredDuration),
        counters.Overwrite.CreateReport("Overwrite", measuredDuration)
    };

    var totalOperations = operationReports.Sum(operation => operation.Operations);
    var totalFailures = operationReports.Sum(operation => operation.Failures);
    var totalReads = Interlocked.Read(ref counters.Hits) + Interlocked.Read(ref counters.Misses);
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
        ThinkTimeMilliseconds: profile.ThinkTimeMilliseconds,
        DurationSeconds: measuredDuration.TotalSeconds,
        Hits: Interlocked.Read(ref counters.Hits),
        Misses: Interlocked.Read(ref counters.Misses),
        FactoryExecutions: Interlocked.Read(ref counters.FactoryExecutions),
        TotalOperations: totalOperations,
        Failures: totalFailures,
        HitRatio: totalReads == 0 ? 0 : (double)counters.Hits / totalReads,
        OperationsPerSecond: measuredDuration.TotalSeconds <= 0 ? 0 : totalOperations / measuredDuration.TotalSeconds,
        ProcessCpuMilliseconds: processMetrics.CpuMilliseconds,
        AllocatedBytes: processMetrics.AllocatedBytes,
        Gen0Collections: processMetrics.Gen0Collections,
        Gen1Collections: processMetrics.Gen1Collections,
        Gen2Collections: processMetrics.Gen2Collections,
        Operations: operationReports,
        FailureSamples: counters.FailureSamples.ToArray());

    var reportFolder = Path.Combine("reports", providerKind.ToString().ToLowerInvariant());
    Directory.CreateDirectory(reportFolder);

    await File.WriteAllTextAsync(
        Path.Combine(reportFolder, "load-summary.json"),
        JsonSerializer.Serialize(summary, CachingLoadJsonContext.Default.LoadTestSummary));
    await File.WriteAllTextAsync(Path.Combine(reportFolder, "load-report.md"), BuildMarkdownReport(summary));
    await File.WriteAllTextAsync(Path.Combine(reportFolder, "load-report.csv"), BuildCsvReport(summary));
    await File.WriteAllTextAsync(Path.Combine(reportFolder, "load-report.html"), BuildHtmlReport(summary));

    Console.WriteLine(
        $"Completed {providerKind}: {summary.TotalOperations:N0} operations, " +
        $"{summary.OperationsPerSecond:N0} ops/s, {summary.HitRatio:P2} read hit ratio, " +
        $"{summary.Failures:N0} failures.");

    if (summary.Failures > 0)
    {
        foreach (var failure in summary.FailureSamples)
            Console.Error.WriteLine($"Workload failure: {failure}");
        Environment.ExitCode = 1;
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

static async Task<TimeSpan> RunWorkloadAsync(
    ICacheProvider cache,
    ICacheWriter writer,
    IReadOnlyList<Guid> tenantIds,
    IReadOnlyList<string[]> keys,
    IReadOnlyList<LoadPayload[]> payloads,
    LoadProfile profile,
    LoadCounters counters)
{
    var startedAt = Stopwatch.GetTimestamp();
    var rampUp = TimeSpan.FromSeconds(profile.RampUpSeconds);
    var sustained = TimeSpan.FromSeconds(profile.SustainedSeconds);
    var rampDown = TimeSpan.FromSeconds(profile.RampDownSeconds);
    var workerTasks = Enumerable.Range(0, profile.VirtualUsers)
        .Select(workerId => RunWorkerAsync(
            workerId, startedAt, rampUp, sustained, rampDown,
            cache, writer, tenantIds, keys, payloads, profile, counters))
        .ToArray();

    await Task.WhenAll(workerTasks);
    return Stopwatch.GetElapsedTime(startedAt);
}

static async Task RunWorkerAsync(
    int workerId,
    long startedAt,
    TimeSpan rampUp,
    TimeSpan sustained,
    TimeSpan rampDown,
    ICacheProvider cache,
    ICacheWriter writer,
    IReadOnlyList<Guid> tenantIds,
    IReadOnlyList<string[]> keys,
    IReadOnlyList<LoadPayload[]> payloads,
    LoadProfile profile,
    LoadCounters counters)
{
    var random = new Random(0x5EED + workerId);
    var workerCount = profile.VirtualUsers;
    var rampDelay = workerCount <= 1 ? TimeSpan.Zero : TimeSpan.FromTicks(rampUp.Ticks * workerId / workerCount);
    if (rampDelay > TimeSpan.Zero)
        await Task.Delay(rampDelay);

    var workerStopAfter = rampUp + sustained +
        TimeSpan.FromTicks(rampDown.Ticks * (workerId + 1) / workerCount);

    while (Stopwatch.GetElapsedTime(startedAt) < workerStopAfter)
    {
        var tenantIndex = random.Next(profile.TenantCount);
        var keyIndex = SelectSkewedKey(random, profile.KeysPerTenant);
        var operation = random.Next(100);
        var key = keys[tenantIndex][keyIndex];
        var payload = payloads[tenantIndex][keyIndex];
        var started = Stopwatch.GetTimestamp();

        if (operation < 80)
        {
            try
            {
                var result = await cache.GetOrSetAsync(
                    key,
                    _ =>
                    {
                        Interlocked.Increment(ref counters.FactoryExecutions);
                        return Task.FromResult(payload);
                    });

                if (result.Value.TenantId != tenantIds[tenantIndex] || result.Value.KeyId != keyIndex)
                    throw new InvalidOperationException("Tenant/key isolation mismatch in cached payload.");

                if (result.IsHit)
                    Interlocked.Increment(ref counters.Hits);
                else
                    Interlocked.Increment(ref counters.Misses);

                counters.Read.RecordSuccess(Stopwatch.GetElapsedTime(started));
            }
            catch (Exception exception)
            {
                counters.RecordFailure("read", exception, Stopwatch.GetElapsedTime(started));
            }
        }
        else if (operation < 95)
        {
            try
            {
                await writer.SetAsync(key, payload);
                counters.Write.RecordSuccess(Stopwatch.GetElapsedTime(started));
            }
            catch (Exception exception)
            {
                counters.RecordFailure("write", exception, Stopwatch.GetElapsedTime(started));
            }
        }
        else
        {
            try
            {
                await writer.SetAsync(keys[tenantIndex][0], payloads[tenantIndex][0]);
                counters.Overwrite.RecordSuccess(Stopwatch.GetElapsedTime(started));
            }
            catch (Exception exception)
            {
                counters.RecordFailure("overwrite", exception, Stopwatch.GetElapsedTime(started));
            }
        }

        if (profile.ThinkTimeMilliseconds > 0)
            await Task.Delay(random.Next(profile.ThinkTimeMilliseconds, profile.ThinkTimeMilliseconds * 2 + 1));
    }
}

static int SelectSkewedKey(Random random, int keyCount)
{
    // Power-law-like skew creates a repeatable hot working set without external traces.
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
    var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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

    start.SetResult(true);
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

static string BuildMarkdownReport(LoadTestSummary summary)
{
    var builder = new StringBuilder();
    builder.AppendLine($"# Caching workload report — {summary.Provider}");
    builder.AppendLine();
    builder.AppendLine($"- Profile: {summary.Profile}");
    builder.AppendLine($"- Commit: {summary.Commit}");
    builder.AppendLine($"- Runtime: {summary.Runtime}");
    builder.AppendLine($"- OS: {summary.OperatingSystem}");
    builder.AppendLine($"- Processors: {summary.ProcessorCount}");
    builder.AppendLine($"- Tenants / keys per tenant: {summary.TenantCount:N0} / {summary.KeysPerTenant:N0}");
    builder.AppendLine($"- Prewarmed keys: {summary.PrewarmedKeys:N0}");
    builder.AppendLine($"- Virtual users: {summary.VirtualUsers:N0}");
    builder.AppendLine($"- Think time: {summary.ThinkTimeMilliseconds}–{summary.ThinkTimeMilliseconds * 2} ms");
    builder.AppendLine($"- Duration: {summary.DurationSeconds:F1} s");
    builder.AppendLine($"- Total throughput: {summary.OperationsPerSecond:N0} ops/s");
    builder.AppendLine($"- Read hit ratio: {summary.HitRatio:P2}");
    builder.AppendLine($"- Failures: {summary.Failures:N0}");
    builder.AppendLine();
    builder.AppendLine("| Operation | Count | Failures | RPS | Mean ms | p50 ms | p95 ms | p99 ms | Max ms |");
    builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
    foreach (var operation in summary.Operations)
        builder.AppendLine($"| {operation.Name} | {operation.Operations:N0} | {operation.Failures:N0} | {operation.RequestsPerSecond:N0} | {operation.MeanLatencyMs:F3} | {operation.P50LatencyMs:F3} | {operation.P95LatencyMs:F3} | {operation.P99LatencyMs:F3} | {operation.MaxLatencyMs:F3} |");
    builder.AppendLine();
    builder.AppendLine($"- Process CPU time: {summary.ProcessCpuMilliseconds:N0} ms");
    builder.AppendLine($"- Managed allocations: {summary.AllocatedBytes:N0} bytes");
    builder.AppendLine($"- GC collections (Gen0/1/2): {summary.Gen0Collections}/{summary.Gen1Collections}/{summary.Gen2Collections}");
    return builder.ToString();
}

static string BuildCsvReport(LoadTestSummary summary)
{
    var builder = new StringBuilder();
    builder.AppendLine("provider,profile,commit,operation,count,failures,rps,mean_ms,p50_ms,p95_ms,p99_ms,max_ms");
    foreach (var operation in summary.Operations)
        builder.AppendLine($"{summary.Provider},{summary.Profile},{summary.Commit},{operation.Name},{operation.Operations},{operation.Failures},{operation.RequestsPerSecond:F3},{operation.MeanLatencyMs:F3},{operation.P50LatencyMs:F3},{operation.P95LatencyMs:F3},{operation.P99LatencyMs:F3},{operation.MaxLatencyMs:F3}");
    return builder.ToString();
}

static string BuildHtmlReport(LoadTestSummary summary)
{
    var builder = new StringBuilder();
    builder.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
    builder.AppendLine("<title>Franz.Common Caching workload report</title>");
    builder.AppendLine("<style>body{font:15px system-ui,sans-serif;max-width:1100px;margin:2rem auto;padding:0 1rem;color:#17202a}table{border-collapse:collapse;width:100%;margin:1rem 0}th,td{border:1px solid #ccd1d1;padding:.55rem;text-align:right}th:first-child,td:first-child{text-align:left}th{background:#f2f4f4}.meta{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:.5rem}.meta div{background:#f4f6f6;padding:.75rem;border-radius:6px}</style></head><body>");
    builder.AppendLine($"<h1>Caching workload report — {WebUtility.HtmlEncode(summary.Provider)}</h1>");
    builder.AppendLine($"<p>Profile <strong>{WebUtility.HtmlEncode(summary.Profile)}</strong> · Commit <code>{WebUtility.HtmlEncode(summary.Commit)}</code></p>");
    builder.AppendLine("<section class=\"meta\">");
    builder.AppendLine($"<div><strong>{summary.OperationsPerSecond:N0}</strong><br>operations / second</div>");
    builder.AppendLine($"<div><strong>{summary.HitRatio:P2}</strong><br>read hit ratio</div>");
    builder.AppendLine($"<div><strong>{summary.Failures:N0}</strong><br>failed operations</div>");
    builder.AppendLine($"<div><strong>{summary.DurationSeconds:F1}s</strong><br>measured duration</div></section>");
    builder.AppendLine("<table><thead><tr><th>Operation</th><th>Count</th><th>Failures</th><th>RPS</th><th>Mean ms</th><th>p50 ms</th><th>p95 ms</th><th>p99 ms</th><th>Max ms</th></tr></thead><tbody>");
    foreach (var operation in summary.Operations)
        builder.AppendLine($"<tr><td>{operation.Name}</td><td>{operation.Operations:N0}</td><td>{operation.Failures:N0}</td><td>{operation.RequestsPerSecond:N0}</td><td>{operation.MeanLatencyMs:F3}</td><td>{operation.P50LatencyMs:F3}</td><td>{operation.P95LatencyMs:F3}</td><td>{operation.P99LatencyMs:F3}</td><td>{operation.MaxLatencyMs:F3}</td></tr>");
    builder.AppendLine("</tbody></table>");
    builder.AppendLine($"<p>Runtime: {WebUtility.HtmlEncode(summary.Runtime)} · OS: {WebUtility.HtmlEncode(summary.OperatingSystem)} · CPU count: {summary.ProcessorCount}</p>");
    builder.AppendLine($"<p>Managed allocations: {summary.AllocatedBytes:N0} bytes · GC collections: {summary.Gen0Collections}/{summary.Gen1Collections}/{summary.Gen2Collections}</p>");
    builder.AppendLine("</body></html>");
    return builder.ToString();
}

internal enum CacheProviderKind { Memory, Distributed, Hybrid, Redis }

internal sealed record LoadProfile(
    string Name,
    int TenantCount,
    int KeysPerTenant,
    int PrewarmPercentage,
    int VirtualUsers,
    int WarmUpSeconds,
    int RampUpSeconds,
    int SustainedSeconds,
    int RampDownSeconds,
    int ThinkTimeMilliseconds,
    int SetupParallelism)
{
    public static bool TryCreate(string name, out LoadProfile profile)
    {
        profile = name.ToLowerInvariant() switch
        {
            "ci" => new("ci", 32, 250, 30, 24, 5, 5, 45, 5, 5, 24),
            "full" => new("full", 100, 1_000, 30, 100, 30, 30, 600, 30, 5, 48),
            "stress" => new("stress", 100, 1_000, 30, 256, 30, 60, 1_200, 60, 2, 64),
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
    private const int MaximumFailureSamples = 10;
    private int _failureSampleCount;

    public OperationMetrics Read { get; } = new();
    public OperationMetrics Write { get; } = new();
    public OperationMetrics Overwrite { get; } = new();
    public ConcurrentQueue<string> FailureSamples { get; } = new();
    public long Hits;
    public long Misses;
    public long FactoryExecutions;

    public long Failures => Read.Failures + Write.Failures + Overwrite.Failures;

    public void RecordFailure(string operation, Exception exception, TimeSpan elapsed)
    {
        var metrics = operation switch
        {
            "read" => Read,
            "write" => Write,
            "overwrite" => Overwrite,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        metrics.RecordFailure(elapsed);

        if (Interlocked.Increment(ref _failureSampleCount) <= MaximumFailureSamples)
            FailureSamples.Enqueue($"{operation}: {exception.GetType().Name}: {exception.Message}");
    }
}

internal sealed class OperationMetrics
{
    private readonly ConcurrentQueue<double> _latencySamples = new();
    private long _operations;
    private long _failures;
    private long _totalLatencyMicroseconds;
    private long _maxLatencyMicroseconds;

    public long Operations => Interlocked.Read(ref _operations);
    public long Failures => Interlocked.Read(ref _failures);

    public void RecordSuccess(TimeSpan elapsed) => Record(elapsed, failed: false);
    public void RecordFailure(TimeSpan elapsed) => Record(elapsed, failed: true);

    private void Record(TimeSpan elapsed, bool failed)
    {
        var operationNumber = Interlocked.Increment(ref _operations);
        if (failed)
            Interlocked.Increment(ref _failures);

        var microseconds = Math.Max(0, (long)(elapsed.TotalMilliseconds * 1_000));
        Interlocked.Add(ref _totalLatencyMicroseconds, microseconds);

        var currentMaximum = Interlocked.Read(ref _maxLatencyMicroseconds);
        while (microseconds > currentMaximum)
        {
            var observed = Interlocked.CompareExchange(ref _maxLatencyMicroseconds, microseconds, currentMaximum);
            if (observed == currentMaximum)
                break;
            currentMaximum = observed;
        }

        if (operationNumber % 100 == 0)
            _latencySamples.Enqueue(elapsed.TotalMilliseconds);
    }

    public OperationReport CreateReport(string name, TimeSpan duration)
    {
        var operations = Operations;
        var failures = Failures;
        var mean = operations == 0 ? 0 : (double)Interlocked.Read(ref _totalLatencyMicroseconds) / 1_000 / operations;
        var samples = _latencySamples.ToArray();
        Array.Sort(samples);

        return new OperationReport(
            Name: name,
            Operations: operations,
            Failures: failures,
            RequestsPerSecond: duration.TotalSeconds <= 0 ? 0 : operations / duration.TotalSeconds,
            MeanLatencyMs: mean,
            P50LatencyMs: Percentile(samples, 0.50, mean),
            P95LatencyMs: Percentile(samples, 0.95, mean),
            P99LatencyMs: Percentile(samples, 0.99, mean),
            MaxLatencyMs: (double)Interlocked.Read(ref _maxLatencyMicroseconds) / 1_000);
    }

    private static double Percentile(double[] samples, double percentile, double fallback)
    {
        if (samples.Length == 0)
            return fallback;

        var index = Math.Clamp((int)Math.Ceiling(percentile * samples.Length) - 1, 0, samples.Length - 1);
        return samples[index];
    }
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

internal sealed record OperationReport(
    string Name,
    long Operations,
    long Failures,
    double RequestsPerSecond,
    double MeanLatencyMs,
    double P50LatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    double MaxLatencyMs);

internal sealed record LoadTestSummary(
    string Provider,
    string Profile,
    string Commit,
    string Runtime,
    string OperatingSystem,
    int ProcessorCount,
    int TenantCount,
    int KeysPerTenant,
    int PrewarmedKeys,
    int VirtualUsers,
    int ThinkTimeMilliseconds,
    double DurationSeconds,
    long Hits,
    long Misses,
    long FactoryExecutions,
    long TotalOperations,
    long Failures,
    double HitRatio,
    double OperationsPerSecond,
    double ProcessCpuMilliseconds,
    long AllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    OperationReport[] Operations,
    string[] FailureSamples);

internal sealed record LoadRequest(int KeyId);
internal sealed record LoadPayload(Guid TenantId, int KeyId, string Content);

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(LoadRequest))]
[JsonSerializable(typeof(LoadPayload))]
[JsonSerializable(typeof(LoadTestSummary))]
internal partial class CachingLoadJsonContext : JsonSerializerContext { }
