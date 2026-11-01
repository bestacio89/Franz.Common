using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.SQLServer.Tests;

public class SqlServerPersistenceTests : IClassFixture<SqlServerFixture>
{
  private readonly SqlServerFixture _fixture;

  public SqlServerPersistenceTests(SqlServerFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task AddAsync_WithSqlServer_ShouldApplyAuditingAndPatternReplacement()
  {
    var domainId = Guid.NewGuid();
    var sp = _fixture.BuildServiceProvider(
        domainId.ToString(),
        databaseName: "franz_{dbName}",
        isolationMode: "DomainDatabase");
    using var scope = sp.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, TestEntity>>();

    await db.Database.EnsureCreatedAsync();

    var connectionString = db.Database.GetConnectionString();
    connectionString.Should().Contain($"Initial Catalog=franz_{domainId:D}");

    var entity = factory.Create();
    entity.Name = "SQL Server Architect";
    var operationTime = DateTimeOffset.UtcNow;

    db.TestEntities.Add(entity);
    await db.SaveChangesAsync();

    var saved = await db.TestEntities.FindAsync(entity.Id);
    saved.Should().NotBeNull();
    saved!.DateCreated.Should().BeCloseTo(operationTime, TimeSpan.FromSeconds(2));
    saved.CreatedBy.Should().Be("system");
  }

  [Fact]
  public async Task SoftDelete_OnSqlServer_ShouldRespectGlobalQueryFilter()
  {
    var sp = _fixture.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    await db.Database.EnsureCreatedAsync();

    var entity = new TestEntity { Name = "Delete Me" };
    db.TestEntities.Add(entity);
    await db.SaveChangesAsync();

    db.TestEntities.Remove(entity);
    await db.SaveChangesAsync();

    using var verifyScope = sp.CreateScope();
    var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();

    var filtered = await verifyDb.TestEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
    var raw = await verifyDb.TestEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entity.Id);

    filtered.Should().BeNull();
    raw.Should().NotBeNull();
    raw!.IsDeleted.Should().BeTrue();
    raw.DateDeleted.Should().NotBe(default);
  }

  [Fact]
  public void AddSqlServerDatabase_UsesTenantIdAndNeverFallsBackToDomainId()
  {
    var tenantId = Guid.NewGuid();
    var domainId = Guid.NewGuid();
    var context = new TenantExecutionContext(
        new TenantInfo(tenantId, "tenant"),
        domainId,
        TenantResolutionSource.Unknown,
        DateTimeOffset.UtcNow);
    var accessor = new StubTenantExecutionContextAccessor(context);

    var provider = _fixture.BuildServiceProvider(
        domainId.ToString(),
        services => services.AddSingleton<ITenantExecutionContextAccessor>(accessor),
        databaseName: "franz_{dbName}",
        isolationMode: "TenantDatabase");

    var connectionString = provider.GetRequiredService<TestDbContext>().Database.GetConnectionString();

    connectionString.Should().Contain($"Initial Catalog=franz_{tenantId:D}");
    connectionString.Should().NotContain(domainId.ToString());
  }

  private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current)
      : ITenantExecutionContextAccessor
  {
    public TenantExecutionContext? Current { get; private set; } = current;
    public void Initialize(TenantExecutionContext context) => Current = context;
    public void Clear() => Current = null;
  }
}
