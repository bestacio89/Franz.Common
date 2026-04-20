using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Business.Domain.Factories;

namespace Franz.Common.EntityFramework.SQLServer.Tests;

public class SqlServerPersistenceTests : IClassFixture<SqlServerFixture>
{
  private readonly SqlServerFixture _fixture;

  public SqlServerPersistenceTests(SqlServerFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task AddAsync_WithSqlServer_ShouldApplyAuditingAndPatternReplacement()
  {
    // Arrange
    var domainId = Guid.NewGuid();
    var sp = _fixture.BuildServiceProvider(domainId.ToString());
    using var scope = sp.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, TestEntity>>();

    await db.Database.EnsureCreatedAsync();

    // Act: Verify Connection String replacement
    var connectionString = db.Database.GetConnectionString();
    connectionString.Should().Contain($"Initial Catalog=franz_{domainId}");

    // Act: Persistence
    var entity = factory.Create();
    entity.Name = "SQL Server Architect";
    var operationTime = DateTimeOffset.UtcNow;

    db.TestEntities.Add(entity);
    await db.SaveChangesAsync();

    // Assert
    var saved = await db.TestEntities.FindAsync(entity.Id);
    saved.Should().NotBeNull();
    saved!.DateCreated.Should().BeCloseTo(operationTime, TimeSpan.FromSeconds(2));
    saved.CreatedBy.Should().Be("system");
  }

  [Fact]
  public async Task SoftDelete_OnSqlServer_ShouldRespectGlobalQueryFilter()
  {
    // Arrange
    var sp = _fixture.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    await db.Database.EnsureCreatedAsync();

    var entity = new TestEntity { Name = "Delete Me" };
    db.TestEntities.Add(entity);
    await db.SaveChangesAsync();

    // Act
    db.TestEntities.Remove(entity);
    await db.SaveChangesAsync();

    // Assert
    // Re-query in fresh scope to bypass local cache
    using var verifyScope = sp.CreateScope();
    var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();

    var filtered = await verifyDb.TestEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
    var raw = await verifyDb.TestEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entity.Id);

    filtered.Should().BeNull();
    raw.Should().NotBeNull();
    raw!.IsDeleted.Should().BeTrue();
    raw.DateDeleted.Should().NotBe(default);
  }
}