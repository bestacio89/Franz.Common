using FluentAssertions;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public sealed class TenantIsolationBoundaryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public TenantIsolationBoundaryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TenantOwnedReads_FailClosedWithoutExecutionContext()
    {
        var tenantId = Guid.NewGuid();

        await using (var provider = BuildTenantProvider(tenantId))
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.TenantOwnedEntities.Add(new TenantOwnedTestEntity { Name = $"visible-only-to-{tenantId:N}" });
            await context.SaveChangesAsync();
        }

        await using var unscopedProvider = (ServiceProvider)_fixture.BuildServiceProvider();
        await using var unscopedScope = unscopedProvider.CreateAsyncScope();
        var unscopedContext = unscopedScope.ServiceProvider.GetRequiredService<TestDbContext>();

        var visible = await unscopedContext.TenantOwnedEntities
            .AsNoTracking()
            .Where(entity => entity.Name == $"visible-only-to-{tenantId:N}")
            .ToListAsync();

        visible.Should().BeEmpty();
    }

    [Fact]
    public async Task TenantOwnedWrites_RejectMissingExecutionContext()
    {
        await using var provider = (ServiceProvider)_fixture.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.TenantOwnedEntities.Add(new TenantOwnedTestEntity { Name = $"must-not-persist-{Guid.NewGuid():N}" });

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<TenantIsolationException>();
    }

    [Fact]
    public async Task TenantOwnedWrites_RejectDetachedEntityClaimingAnotherTenant()
    {
        var tenantId = Guid.NewGuid();
        var foreignTenantId = Guid.NewGuid();

        await using var provider = BuildTenantProvider(tenantId);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var foreignEntity = new TenantOwnedTestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = foreignTenantId,
            Name = "foreign-tenant"
        };

        context.TenantOwnedEntities.Update(foreignEntity);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<TenantIsolationException>();
    }

    private ServiceProvider BuildTenantProvider(Guid tenantId)
    {
        var tenant = new TenantInfo(tenantId, "tenant");
        var executionContext = new TenantExecutionContext(
            tenant, null, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        var accessor = new StubTenantExecutionContextAccessor(executionContext);

        return (ServiceProvider)_fixture.BuildServiceProvider(
            extraRegistrations: services => services.AddSingleton<ITenantExecutionContextAccessor>(accessor));
    }

    private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current)
        : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;

        public void Initialize(TenantExecutionContext context) => Current = context;

        public void Clear() => Current = null;
    }
}
