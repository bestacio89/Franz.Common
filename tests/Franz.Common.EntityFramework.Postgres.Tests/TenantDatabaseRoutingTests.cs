using FluentAssertions;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public sealed class TenantDatabaseRoutingTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public TenantDatabaseRoutingTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void AddPostgresDatabase_UsesValidatedTenantIdWhenTenantIsolationIsEnabled()
    {
        var tenantId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var tenant = new TenantInfo(tenantId, "tenant");
        var context = new TenantExecutionContext(tenant, domainId, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        var accessor = new StubTenantExecutionContextAccessor(context);

        var provider = _fixture.BuildServiceProvider(
            domainId.ToString(),
            services => services.AddSingleton<ITenantExecutionContextAccessor>(accessor),
            databaseName: "test_{dbName}",
            isolationMode: "TenantDatabase");

        var connectionString = provider.GetRequiredService<TestDbContext>().Database.GetConnectionString();

        connectionString.Should().Contain($"Database=test_{tenantId:D}");
        connectionString.Should().NotContain(domainId.ToString());
    }

    [Fact]
    public void AddPostgresDatabase_UsesDomainRoutingOnlyWhenExplicitlyEnabled()
    {
        var domainId = Guid.NewGuid();
        var provider = _fixture.BuildServiceProvider(
            domainId.ToString(),
            databaseName: "test_{dbName}",
            isolationMode: "DomainDatabase");

        var connectionString = provider.GetRequiredService<TestDbContext>().Database.GetConnectionString();

        connectionString.Should().Contain($"Database=test_{domainId:D}");
    }

    [Fact]
    public void AddPostgresDatabase_PreservesSharedDatabaseWhenNoTenantTemplateIsConfigured()
    {
        var provider = _fixture.BuildServiceProvider(databaseName: "shared");

        var connectionString = provider.GetRequiredService<TestDbContext>().Database.GetConnectionString();

        connectionString.Should().Contain("Database=shared");
    }

    [Fact]
    public async Task TenantOwnedRows_AreIsolatedAcrossRealPostgresTenantContexts()
    {
        const string databaseName = "franz_test";
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var tenantAProvider = BuildTenantProvider(databaseName, tenantA))
        await using (var tenantAScope = tenantAProvider.CreateAsyncScope())
        {
            var context = tenantAScope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.TenantOwnedEntities.Add(new TenantOwnedTestEntity { Name = "tenant-a" });
            await context.SaveChangesAsync();
        }

        await using (var tenantBProvider = BuildTenantProvider(databaseName, tenantB))
        await using (var tenantBScope = tenantBProvider.CreateAsyncScope())
        {
            var context = tenantBScope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.TenantOwnedEntities.Add(new TenantOwnedTestEntity { Name = "tenant-b" });
            await context.SaveChangesAsync();
        }

        await using (var tenantAProvider = BuildTenantProvider(databaseName, tenantA))
        await using (var tenantAScope = tenantAProvider.CreateAsyncScope())
        {
            var context = tenantAScope.ServiceProvider.GetRequiredService<TestDbContext>();
            var rows = await context.TenantOwnedEntities.AsNoTracking().ToListAsync();

            rows.Should().ContainSingle();
            rows[0].Name.Should().Be("tenant-a");
            rows.Should().OnlyContain(row => row.TenantId == tenantA);
        }

        await using (var tenantBProvider = BuildTenantProvider(databaseName, tenantB))
        await using (var tenantBScope = tenantBProvider.CreateAsyncScope())
        {
            var context = tenantBScope.ServiceProvider.GetRequiredService<TestDbContext>();
            var rows = await context.TenantOwnedEntities.AsNoTracking().ToListAsync();

            rows.Should().ContainSingle();
            rows[0].Name.Should().Be("tenant-b");
            rows.Should().OnlyContain(row => row.TenantId == tenantB);
        }
    }

    [Fact]
    public async Task TenantOwnedWrites_FailClosedWithoutValidatedTenantContext()
    {
        var provider = _fixture.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await context.Database.EnsureCreatedAsync();

        context.TenantOwnedEntities.Add(new TenantOwnedTestEntity { Name = "unscoped" });

        var act = () => context.SaveChangesAsync();
        await act.Should().ThrowAsync<TenantIsolationException>();
    }

    private ServiceProvider BuildTenantProvider(string databaseName, Guid tenantId)
    {
        var tenant = new TenantInfo(tenantId, "tenant");
        var context = new TenantExecutionContext(tenant, null, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        var accessor = new StubTenantExecutionContextAccessor(context);

        return (ServiceProvider)_fixture.BuildServiceProvider(
            extraRegistrations: services => services.AddSingleton<ITenantExecutionContextAccessor>(accessor),
            databaseName: databaseName);
    }

    private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current) : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;
        public void Initialize(TenantExecutionContext context) => Current = context;
        public void Clear() => Current = null;
    }
}
