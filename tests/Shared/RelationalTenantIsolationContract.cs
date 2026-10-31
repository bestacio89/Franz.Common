using Franz.Common.Business.Domain;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Franz.Common.EntityFramework.ProviderContract.Tests;

public sealed class TenantOwnedTestEntity : Entity<Guid>, ITenantEntity
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

    [Fact]
    public async Task TenantOwnedQueries_AreRestrictedToTheExecutionTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var nameA = $"tenant-a-{Guid.NewGuid():N}";
        var nameB = $"tenant-b-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await db.Database.EnsureCreatedAsync();
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
            await db.Database.EnsureCreatedAsync();
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
            await db.Database.EnsureCreatedAsync();
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
            await db.Database.EnsureCreatedAsync();
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
            await db.Database.EnsureCreatedAsync();
            var entity = new TenantOwnedTestEntity { Name = $"ownership-{Guid.NewGuid():N}" };
            db.Set<TenantOwnedTestEntity>().Add(entity);
            await db.SaveChangesAsync();

            entity.TenantId = foreignTenantId;

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
            await db.Database.EnsureCreatedAsync();
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
    public async Task ExecuteDeleteAsync_RespectsTenantQueryFilters()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var marker = $"bulk-delete-{Guid.NewGuid():N}";

        await WithContextAsync(tenantA, async db =>
        {
            await db.Database.EnsureCreatedAsync();
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
