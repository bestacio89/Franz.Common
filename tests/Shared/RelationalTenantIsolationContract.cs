using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Franz.Common.EntityFramework.ProviderContract.Tests;

public sealed class TenantOwnedTestEntity : ConcurrencyTrackedEntity<Guid>, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Shared relational behavior contract. Each concrete provider suite supplies
/// a real-engine DbContext and owns its DI/database lifetime.
/// </summary>
public abstract class RelationalTenantIsolationContract
{
    protected abstract Task WithContextAsync(Guid? tenantId, Func<DbContext, Task> action);

    protected virtual Task EnsureDatabaseCreatedAsync(DbContext db)
        => db.Database.EnsureCreatedAsync();

    [Fact]
    public async Task TenantOwnedQueries_AreRestrictedToTheExecutionTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var nameA = $"tenant-a-{Guid.NewGuid():N}";
        var nameB = $"tenant-b-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = nameA });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantB, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = nameB });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantA, async db =>
        {
            var rows = await db.Set<TenantOwnedTestEntity>().AsNoTracking().ToListAsync();
            Assert.Equal(nameA, Assert.Single(rows).Name);
        });

        await WithContextAsync(null, async db =>
        {
            var rows = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .Where(x => x.Name == nameA || x.Name == nameB)
                .ToListAsync();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public async Task TenantOwnedWrites_RejectMissingExecutionTenant()
    {
        await WithContextAsync(null, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(
                new TenantOwnedTestEntity { Name = $"unscoped-{Guid.NewGuid():N}" });

            await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task TenantOwnedWrites_RejectForeignTenantOwnershipClaims()
    {
        var tenantId = Guid.NewGuid();
        var foreignTenantId = Guid.NewGuid();

        await WithContextAsync(tenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity
            {
                TenantId = foreignTenantId,
                Name = $"foreign-{Guid.NewGuid():N}"
            });

            await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task TenantOwnedUpdates_RejectDetachedEntitiesFromAnotherTenant()
    {
        var ownerTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var marker = $"detached-update-{Guid.NewGuid():N}";
        TenantOwnedTestEntity? detached = null;

        await WithContextAsync(ownerTenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            var entity = new TenantOwnedTestEntity { Name = marker };
            db.Set<TenantOwnedTestEntity>().Add(entity);
            await db.SaveChangesAsync();
            detached = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(x => x.Name == marker);
        });

        await WithContextAsync(otherTenantId, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Update(detached!);
            await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task TenantOwnedUpdates_RejectOwnershipMutation()
    {
        var tenantId = Guid.NewGuid();
        var foreignTenantId = Guid.NewGuid();

        await WithContextAsync(tenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            var entity = new TenantOwnedTestEntity { Name = $"ownership-{Guid.NewGuid():N}" };
            db.Set<TenantOwnedTestEntity>().Add(entity);
            await db.SaveChangesAsync();

            entity.TenantId = foreignTenantId;

            await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task TenantOwnedDeletes_RejectEntitiesOwnedByAnotherTenant()
    {
        var ownerTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var marker = $"detached-delete-{Guid.NewGuid():N}";
        TenantOwnedTestEntity? detached = null;

        await WithContextAsync(ownerTenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            var entity = new TenantOwnedTestEntity { Name = marker };
            db.Set<TenantOwnedTestEntity>().Add(entity);
            await db.SaveChangesAsync();
            detached = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(x => x.Name == marker);
        });

        await WithContextAsync(otherTenantId, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Remove(detached!);
            await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task ExecuteUpdateAsync_RespectsTenantQueryFilters()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var marker = $"bulk-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantB, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantA, async db =>
        {
            var affected = await db.Set<TenantOwnedTestEntity>()
                .Where(x => x.Name == marker)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Name, "updated-by-tenant-a"));

            Assert.Equal(1, affected);
        });

        await WithContextAsync(tenantB, async db =>
        {
            var row = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(x => x.Name == marker);

            Assert.Equal(tenantB, row.TenantId);
        });
    }

    [Fact]
    public async Task RepositoryBulkUpdate_RespectsTenantQueryFilters()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var marker = $"repository-bulk-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantB, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantA, async db =>
        {
            var repository = new EntityRepository<DbContextBase, TenantOwnedTestEntity, Guid>(
                (DbContextBase)db);
            var affected = await repository.ExecuteBulkUpdateAsync(
                entity => entity.Name == marker,
                update => update.SetProperty(entity => entity.Name, "updated-by-repository"));

            Assert.Equal(1, affected);
        });

        await WithContextAsync(tenantB, async db =>
        {
            var row = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(entity => entity.Name == marker);

            Assert.Equal(tenantB, row.TenantId);
        });
    }

    [Fact]
    public async Task RepositoryBulkUpdate_RejectsTenantOwnershipMutation()
    {
        var tenantId = Guid.NewGuid();
        var foreignTenantId = Guid.NewGuid();

        await WithContextAsync(tenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            var repository = new EntityRepository<DbContextBase, TenantOwnedTestEntity, Guid>(
                (DbContextBase)db);

            await Assert.ThrowsAsync<TenantIsolationException>(() =>
                repository.ExecuteBulkUpdateAsync(
                    _ => true,
                    update => update.SetProperty(entity => entity.TenantId, foreignTenantId)));
        });
    }

    [Fact]
    public async Task ExecuteDeleteAsync_RespectsTenantQueryFilters()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var marker = $"bulk-delete-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantB, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantA, async db =>
        {
            var affected = await db.Set<TenantOwnedTestEntity>()
                .Where(x => x.Name == marker)
                .ExecuteDeleteAsync();

            Assert.Equal(1, affected);
        });

        await WithContextAsync(tenantB, async db =>
        {
            var remaining = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .Where(x => x.Name == marker)
                .CountAsync();

            Assert.Equal(1, remaining);
        });
    }

    [Fact]
    public async Task UnitOfWork_FailedCommitReleasesItsTransaction()
    {
        var tenantId = Guid.NewGuid();
        var marker = $"uow-failed-commit-{Guid.NewGuid():N}";

        await WithContextAsync(tenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            await using var unitOfWork = new EfUnitOfWork<DbContextBase>((DbContextBase)db);
            await unitOfWork.BeginAsync();

            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ((IUnitOfWork)unitOfWork).CommitAsync(cancellation.Token));

            db.ChangeTracker.Clear();
            await unitOfWork.BeginAsync();
            await unitOfWork.RollbackAsync();
        });

        await WithContextAsync(tenantId, async db =>
        {
            Assert.False(await db.Set<TenantOwnedTestEntity>()
                .IgnoreQueryFilters()
                .AnyAsync(entity => entity.Name == marker));
        });
    }

    [Fact]
    public async Task UnitOfWork_RejectsTransactionsOwnedByTheDbContext()
    {
        await WithContextAsync(Guid.NewGuid(), async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            await using var externalTransaction = await db.Database.BeginTransactionAsync();
            await using var unitOfWork = new EfUnitOfWork<DbContextBase>((DbContextBase)db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.BeginAsync());
            await externalTransaction.RollbackAsync();
        });
    }

    [Fact]
    public async Task UnitOfWork_SynchronousDisposalRollsBackAcrossProviders()
    {
        var tenantId = Guid.NewGuid();
        var marker = $"uow-sync-dispose-{Guid.NewGuid():N}";

        await WithContextAsync(tenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            using var unitOfWork = new EfUnitOfWork<DbContextBase>((DbContextBase)db);
            await unitOfWork.BeginAsync();

            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantId, async db =>
        {
            Assert.False(await db.Set<TenantOwnedTestEntity>()
                .IgnoreQueryFilters()
                .AnyAsync(entity => entity.Name == marker));
        });
    }

    [Fact]
    public async Task ConcurrencyTrackedEntities_RejectStaleUpdates()
    {
        var tenantId = Guid.NewGuid();
        var marker = $"concurrency-{Guid.NewGuid():N}";

        await WithContextAsync(tenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            var entity = new TenantOwnedTestEntity { Name = marker };
            db.Set<TenantOwnedTestEntity>().Add(entity);
            await db.SaveChangesAsync();
            Assert.Equal(1L, entity.ConcurrencyVersion);
        });

        TenantOwnedTestEntity? staleEntity = null;
        await WithContextAsync(tenantId, async db =>
        {
            staleEntity = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(entity => entity.Name == marker);
        });

        await WithContextAsync(tenantId, async db =>
        {
            var current = await db.Set<TenantOwnedTestEntity>()
                .SingleAsync(entity => entity.Name == marker);
            current.Name = "updated-current";
            await db.SaveChangesAsync();
            Assert.Equal(2L, current.ConcurrencyVersion);
        });

        await WithContextAsync(tenantId, async db =>
        {
            staleEntity!.Name = "updated-stale";
            db.Set<TenantOwnedTestEntity>().Update(staleEntity);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        });

        await WithContextAsync(tenantId, async db =>
        {
            var persisted = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(entity => entity.Name == "updated-current");
            Assert.Equal(2L, persisted.ConcurrencyVersion);
        });
    }

    [Fact]
    public void BulkUpdateBuilder_RejectsFrameworkManagedProperties()
    {
        var builder = new BulkUpdateBuilder<TenantOwnedTestEntity>();

        Assert.Throws<InvalidOperationException>(
            () => builder.SetProperty(entity => entity.Id, Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(
            () => builder.SetProperty(entity => entity.IsDeleted, true));
        Assert.Throws<InvalidOperationException>(
            () => builder.SetProperty(entity => entity.LastModifiedBy, "forged"));
        Assert.Throws<InvalidOperationException>(
            () => builder.SetProperty(entity => entity.ConcurrencyVersion, 42L));

        var nonTenantBuilder = new BulkUpdateBuilder<PlainEntity>();
        Assert.Throws<InvalidOperationException>(
            () => nonTenantBuilder.SetProperty(
                entity => EF.Property<string?>(entity, nameof(Entity<object>.LastModifiedBy)),
                "forged"));
    }

    private sealed class PlainEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task RepositoryBulkUpdate_AdvancesConcurrencyVersion()
    {
        var tenantId = Guid.NewGuid();
        var marker = $"bulk-concurrency-{Guid.NewGuid():N}";

        await WithContextAsync(tenantId, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        TenantOwnedTestEntity? staleEntity = null;
        await WithContextAsync(tenantId, async db =>
        {
            staleEntity = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(entity => entity.Name == marker);
        });

        await WithContextAsync(tenantId, async db =>
        {
            var repository = new EntityRepository<DbContextBase, TenantOwnedTestEntity, Guid>(
                (DbContextBase)db);
            var affected = await repository.ExecuteBulkUpdateAsync(
                entity => entity.Name == marker,
                update => update.SetProperty(entity => entity.Name, "bulk-updated"));

            Assert.Equal(1, affected);
        });

        await WithContextAsync(tenantId, async db =>
        {
            var persisted = await db.Set<TenantOwnedTestEntity>()
                .AsNoTracking()
                .SingleAsync(entity => entity.Name == "bulk-updated");
            Assert.Equal(2L, persisted.ConcurrencyVersion);
        });

        await WithContextAsync(tenantId, async db =>
        {
            staleEntity!.Name = "stale-after-bulk";
            db.Set<TenantOwnedTestEntity>().Update(staleEntity);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task RepositoryBulkDelete_SoftDeletesAuditsAndScopesByTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var marker = $"bulk-soft-delete-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantB, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantA, async db =>
        {
            var repository = new EntityRepository<DbContextBase, TenantOwnedTestEntity, Guid>(
                (DbContextBase)db);
            var affected = await repository.ExecuteBulkDeleteAsync(entity => entity.Name == marker);

            Assert.Equal(1, affected);
            Assert.False(await db.Set<TenantOwnedTestEntity>().AnyAsync(entity => entity.Name == marker));

            var deleted = await db.Set<TenantOwnedTestEntity>()
                .IgnoreQueryFilters()
                .SingleAsync(entity => entity.Name == marker && entity.TenantId == tenantA);

            Assert.True(deleted.IsDeleted);
            Assert.Equal("system", deleted.DeletedBy);
            Assert.Equal("system", deleted.LastModifiedBy);
            Assert.NotEqual(default, deleted.DateDeleted);
            Assert.NotEqual(default, deleted.LastModifiedDate);
            Assert.Equal(2L, deleted.ConcurrencyVersion);
        });

        await WithContextAsync(tenantB, async db =>
        {
            var retained = await db.Set<TenantOwnedTestEntity>()
                .SingleAsync(entity => entity.Name == marker);
            Assert.False(retained.IsDeleted);
            Assert.Equal(1L, retained.ConcurrencyVersion);
        });
    }

    [Fact]
    public async Task RepositoryBulkDelete_PhysicallyDeletesOnlyWhenExplicitlyRequested()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var marker = $"bulk-hard-delete-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await EnsureDatabaseCreatedAsync(db);
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantB, async db =>
        {
            db.Set<TenantOwnedTestEntity>().Add(new TenantOwnedTestEntity { Name = marker });
            await db.SaveChangesAsync();
        });

        await WithContextAsync(tenantA, async db =>
        {
            var repository = new EntityRepository<DbContextBase, TenantOwnedTestEntity, Guid>(
                (DbContextBase)db);
            var affected = await repository.ExecuteBulkDeleteAsync(
                entity => entity.Name == marker,
                hardDelete: true);

            Assert.Equal(1, affected);
            Assert.False(await db.Set<TenantOwnedTestEntity>()
                .IgnoreQueryFilters()
                .AnyAsync(entity => entity.Name == marker && entity.TenantId == tenantA));
        });

        await WithContextAsync(tenantB, async db =>
        {
            Assert.True(await db.Set<TenantOwnedTestEntity>()
                .AnyAsync(entity => entity.Name == marker));
        });
    }

}

/// <summary>Test-only accessor factory shared by provider qualification suites.</summary>
public sealed class TenantExecutionContextAccessor(TenantExecutionContext? current)
    : ITenantExecutionContextAccessor
{
    public TenantExecutionContext? Current { get; private set; } = current;
    public void Initialize(TenantExecutionContext context) => Current = context;
    public void Clear() => Current = null;

    public static TenantExecutionContextAccessor ForTenant(Guid? tenantId)
    {
        if (tenantId is null)
            return new TenantExecutionContextAccessor(null);

        var tenant = new TenantInfo(tenantId.Value, "provider-contract-tenant");
        return new TenantExecutionContextAccessor(new TenantExecutionContext(
            tenant, null, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow));
    }
}
