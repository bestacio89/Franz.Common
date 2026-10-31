using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Franz.Common.MultiTenancy.Tests.Persistence;

public sealed class TenantPersistenceIsolationTests
{
    [Fact]
    public async Task QueryFilter_ReturnsOnlyCurrentTenantRows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var context = CreateContext(databaseName, tenantA))
        {
            context.Entities.Add(new TestEntity { Name = "A" });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(databaseName, tenantB))
        {
            context.Entities.Add(new TestEntity { Name = "B" });
            await context.SaveChangesAsync();
        }

        await using var tenantAContext = CreateContext(databaseName, tenantA);
        var rows = await tenantAContext.Entities.ToListAsync();

        Assert.Single(rows);
        Assert.Equal("A", rows[0].Name);
        Assert.All(rows, row => Assert.Equal(tenantA, row.TenantId));
    }

    [Fact]
    public async Task SaveChanges_StampsNewEntityWithCurrentTenant()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString(), Guid.NewGuid());
        var entity = new TestEntity { Name = "stamped" };

        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        Assert.Equal(context.CurrentTenantId, entity.TenantId);
    }

    [Fact]
    public async Task SaveChanges_RejectsExplicitCrossTenantInsert()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString(), Guid.NewGuid());
        context.Entities.Add(new TestEntity { Name = "forged", TenantId = Guid.NewGuid() });

        await Assert.ThrowsAsync<TenantIsolationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_RejectsTenantOwnedWritesWithoutExecutionContext()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString(), null);
        context.Entities.Add(new TestEntity { Name = "unscoped" });

        await Assert.ThrowsAsync<TenantIsolationException>(() => context.SaveChangesAsync());
    }

    private static TestDbContext CreateContext(string databaseName, Guid? tenantId)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new TestDbContext(options, CreateAccessor(tenantId), Substitute.For<IDispatcher>());
    }

    private static ITenantExecutionContextAccessor CreateAccessor(Guid? tenantId)
    {
        var context = tenantId is null
            ? null
            : new TenantExecutionContext(new TenantInfo(tenantId.Value, "tenant"), null, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        return new StubTenantExecutionContextAccessor(context);
    }

    private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current) : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;
        public void Initialize(TenantExecutionContext context) => Current = context;
        public void Clear() => Current = null;
    }

    private sealed class TestDbContext(
        DbContextOptions<TestDbContext> options,
        ITenantExecutionContextAccessor tenantAccessor,
        IDispatcher dispatcher) : DbContextBase(options, dispatcher, tenantContextAccessor: tenantAccessor)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    public sealed class TestEntity : IEntity<Guid>, ITenantEntity
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; private set; }
        public DateTimeOffset DateCreated { get; private set; }
        public object GetId() => Id;
        public void MarkCreated(string createdBy) => DateCreated = DateTimeOffset.UtcNow;
        public void MarkUpdated(string modifiedBy) { }
        public void MarkDeleted(string deletedBy) => IsDeleted = true;
    }
}
