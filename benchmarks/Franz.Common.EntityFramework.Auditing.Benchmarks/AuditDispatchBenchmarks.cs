using BenchmarkDotNet.Attributes;
using Franz.Common.Business.Domain;

namespace Franz.Common.EntityFramework.Auditing.Benchmarks;

/// <summary>
/// Isolates the audit-dispatch portion of DbContextBase.SaveChanges.
/// The legacy case reproduces IEntity discovery plus dynamic lifecycle invocation;
/// the optimized case uses the IAuditableEntity contract directly.
/// </summary>
[MemoryDiagnoser]
public class AuditDispatchBenchmarks
{
    private BenchmarkEntity[] _entities = [];

    [Params(1, 100, 1_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _entities = Enumerable.Range(0, EntityCount)
            .Select(index => new BenchmarkEntity(GuidFromInt(index + 1)))
            .ToArray();
    }

    [Benchmark(Baseline = true, Description = "Legacy reflection + dynamic")]
    public void LegacyReflectionAndDynamicDispatch()
    {
        const string userId = "benchmark-user";

        foreach (var entity in _entities)
        {
            if (entity is not IEntity<object> && !ImplementsIEntity(entity.GetType()))
                continue;

            ((dynamic)entity).MarkCreated(userId);
        }
    }

    [Benchmark(Description = "Direct IAuditableEntity dispatch")]
    public void DirectInterfaceDispatch()
    {
        const string userId = "benchmark-user";

        foreach (IAuditableEntity entity in _entities)
            entity.MarkCreated(userId);
    }

    private static bool ImplementsIEntity(Type type)
        => type.GetInterfaces()
            .Any(candidate =>
                candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IEntity<>));

    private static Guid GuidFromInt(int value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, value);
        return new Guid(bytes);
    }

    private sealed class BenchmarkEntity(Guid id) : Entity<Guid>(id)
    {
    }
}
