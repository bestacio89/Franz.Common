using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
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

    private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current) : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;
        public void Initialize(TenantExecutionContext context) => Current = context;
        public void Clear() => Current = null;
    }
}