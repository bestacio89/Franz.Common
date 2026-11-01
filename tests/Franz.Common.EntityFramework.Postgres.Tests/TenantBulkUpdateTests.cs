using FluentAssertions;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public sealed class TenantBulkUpdateTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public TenantBulkUpdateTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task BulkUpdate_OnlyUpdatesRowsVisibleToCurrentTenant()
    {
        const string databaseName = "franz_test";
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var provider = BuildTenantProvider(databaseName, tenantA))
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.TenantOwnedEntities.Add(new TenantOwnedTestEntity { Name = "before-a" });
            await context.SaveChangesAsync();
        }

        await using (var provider = BuildTenantProvider(databaseName, tenantB))
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.TenantOwnedEntities.Add(new TenantOwnedTestEntity { Name = "before-b" });
            await context.SaveChangesAsync();
        }

        await using (var provider = BuildTenantProvider(databaseName, tenantA))
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var repository = new EntityRepository<TestDbContext, TenantOwnedTestEntity, Guid>(context);

            var affected = await repository.ExecuteBulkUpdateAsync(
                _ => true,
                setters => setters.SetProperty(entity => entity.Name, "after-a"));

            affected.Should().Be(1);
            var visible = await context.TenantOwnedEntities.AsNoTracking().ToListAsync();
            visible.Should().ContainSingle().Which.Name.Should().Be("after-a");
        }

        await using (var provider = BuildTenantProvider(databaseName, tenantB))
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var visible = await context.TenantOwnedEntities.AsNoTracking().ToListAsync();
            visible.Should().ContainSingle().Which.Name.Should().Be("before-b");
        }
    }

    [Fact]
    public async Task BulkUpdate_RejectsTenantOwnershipChanges()
    {
        var tenantId = Guid.NewGuid();
        await using var provider = BuildTenantProvider("franz_test", tenantId);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var repository = new EntityRepository<TestDbContext, TenantOwnedTestEntity, Guid>(context);

        Func<Task> act = async () => await repository.ExecuteBulkUpdateAsync(
            _ => true,
            setters => setters.SetProperty(entity => entity.TenantId, Guid.NewGuid()));

        await act.Should().ThrowAsync<TenantIsolationException>();
    }

    private ServiceProvider BuildTenantProvider(string databaseName, Guid tenantId)
    {
        var tenant = new TenantInfo(tenantId, "tenant");
        var executionContext = new TenantExecutionContext(
            tenant, null, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        var accessor = new StubTenantExecutionContextAccessor(executionContext);

        return (ServiceProvider)_fixture.BuildServiceProvider(
            extraRegistrations: services => services.AddSingleton<ITenantExecutionContextAccessor>(accessor),
            databaseName: databaseName);
    }

    private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current)
        : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;
        public void Initialize(TenantExecutionContext context) => Current = context;
        public void Clear() => Current = null;
    }
}
