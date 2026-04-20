using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public class RepositoryIntegrationTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public RepositoryIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task AddAsync_ShouldApplyAuditAndPersistCorrectId()
  {
    // Arrange
    var sp = _fixture.BuildServiceProvider();
    using var scope = sp.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, TestEntity>>();

    await dbContext.Database.EnsureCreatedAsync();

    var entity = factory.Create();
    entity.Name = "Franz.Common Standard";
    var beforeSave = DateTimeOffset.UtcNow;

    // Act
    dbContext.TestEntities.Add(entity);
    await dbContext.SaveChangesAsync();

    // Assert
    var saved = await dbContext.TestEntities.FindAsync(entity.Id);

    saved.Should().NotBeNull();
    saved!.Id.Should().NotBe(Guid.Empty);

    // Asserting the refined audit properties
    saved.DateCreated.Should().BeCloseTo(beforeSave, TimeSpan.FromSeconds(2));
    saved.CreatedBy.Should().Be("system");
  }

  [Fact]
  public async Task UpdateAsync_ShouldUpdateModificationAudit()
  {
    // Arrange
    var sp = _fixture.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var entity = new TestEntity { Name = "Initial Name" };
    dbContext.TestEntities.Add(entity);
    await dbContext.SaveChangesAsync();

    // Act
    entity.Name = "Updated Name";
    var beforeUpdate = DateTimeOffset.UtcNow;
    await dbContext.SaveChangesAsync();

    // Assert
    var updated = await dbContext.TestEntities.FindAsync(entity.Id);
    updated!.LastModifiedDate.Should().BeCloseTo(beforeUpdate, TimeSpan.FromSeconds(2));
    updated.LastModifiedBy.Should().Be("system");
  }

  [Fact]
  public async Task DeleteAsync_ShouldSoftDeleteAndFilter()
  {
    // Arrange
    var sp = _fixture.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var entity = new TestEntity { Name = "Disposable" };
    dbContext.TestEntities.Add(entity);
    await dbContext.SaveChangesAsync();

    // Act
    var beforeDelete = DateTimeOffset.UtcNow;
    dbContext.TestEntities.Remove(entity);
    await dbContext.SaveChangesAsync();

    // Assert
    var queryResult = await dbContext.TestEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
    var rawResult = await dbContext.TestEntities
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => e.Id == entity.Id);

    queryResult.Should().BeNull(); // Filtered out
    rawResult.Should().NotBeNull();
    rawResult!.IsDeleted.Should().BeTrue();
    rawResult.DateDeleted.Should().BeCloseTo(beforeDelete, TimeSpan.FromSeconds(2));
    rawResult.DeletedBy.Should().Be("system");
  }
}