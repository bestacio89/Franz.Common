using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.AzureCosmosDB.Tests;

public sealed class CosmosIntegrationTests : IClassFixture<CosmosFixture>
{
  private readonly CosmosFixture _fixture;

  public CosmosIntegrationTests(CosmosFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task AddAsync_ShouldPersistJsonDocument_WithAuditMetadata()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();

    Guid entityId;
    var beforeSave = DateTimeOffset.UtcNow;

    using (var scope = testContext.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
      var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

      var entity = factory.Create();
      entity.Label = $"Cosmos Item {Guid.NewGuid():N}";
      entityId = entity.Id;

      db.Items.Add(entity);
      await db.SaveChangesAsync();
    }

    // Fresh tracking isolation context
    using (var verifyScope = testContext.CreateScope())
    {
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
      var saved = await verifyDb.Items.FirstOrDefaultAsync(x => x.Id == entityId);

      saved.Should().NotBeNull();
      saved!.DateCreated.Should().BeCloseTo(beforeSave, TimeSpan.FromSeconds(5));
      saved.CreatedBy.Should().Be("system");
    }

    await testContext.CleanUpAsync();
  }

  [Fact]
  public async Task SoftDelete_ShouldApplyGlobalFilter()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();
    Guid id;

    using (var scope = testContext.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
      var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

      var entity = factory.Create();
      entity.Label = $"Disposable {Guid.NewGuid():N}";

      db.Items.Add(entity);
      await db.SaveChangesAsync();

      id = entity.Id;

      db.Items.Remove(entity);
      await db.SaveChangesAsync();
    }

    using (var verifyScope = testContext.CreateScope())
    {
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

      var filtered = await verifyDb.Items.FirstOrDefaultAsync(x => x.Id == id);
      var raw = await verifyDb.Items
          .IgnoreQueryFilters()
          .FirstOrDefaultAsync(x => x.Id == id);

      filtered.Should().BeNull();
      raw.Should().NotBeNull();
      raw!.IsDeleted.Should().BeTrue();
    }

    await testContext.CleanUpAsync();
  }

  [Fact]
  public async Task GlobalFilter_ShouldExcludeSoftDeleted()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();
    Guid activeId;
    Guid deletedId;

    using (var scope = testContext.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
      var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

      var active = factory.Create();
      active.Label = $"Active {Guid.NewGuid():N}";
      activeId = active.Id;

      var deleted = factory.Create();
      deleted.Label = $"Deleted {Guid.NewGuid():N}";
      deletedId = deleted.Id;

      db.Items.AddRange(active, deleted);
      await db.SaveChangesAsync();

      db.Items.Remove(deleted);
      await db.SaveChangesAsync();
    }

    using (var verifyScope = testContext.CreateScope())
    {
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

      var visible = await verifyDb.Items.ToListAsync();
      visible.Should().ContainSingle(x => x.Id == activeId);

      var all = await verifyDb.Items.IgnoreQueryFilters().ToListAsync();
      all.Should().HaveCount(2);
      all.Should().Contain(x => x.Id == deletedId && x.IsDeleted);
    }

    await testContext.CleanUpAsync();
  }
}