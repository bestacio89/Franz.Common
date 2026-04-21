using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.AzureCosmosDB.Tests;



[Collection("Cosmos")]
[Trait("SkipInCI", "true")]
public class CosmosIntegrationTests : IClassFixture<CosmosFixture>
{
  private readonly CosmosFixture _fixture;

  public CosmosIntegrationTests(CosmosFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task AddAsync_ShouldPersistJsonDocument_WithAuditMetadata()
  {
    using var scope = _fixture.CreateScope();

    var db =
        scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

    var factory =
        scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

    var entity = factory.Create();
    entity.Label = $"Cosmos Item {Guid.NewGuid():N}";

    var beforeSave = DateTimeOffset.UtcNow;

    db.Items.Add(entity);
    await db.SaveChangesAsync();

    db.ChangeTracker.Clear();

    using var verifyScope = _fixture.CreateScope();

    var verifyDb =
        verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

    var saved =
        await verifyDb.Items.FirstOrDefaultAsync(x => x.Id == entity.Id);

    saved.Should().NotBeNull();

    saved!.DateCreated.Should()
        .BeCloseTo(beforeSave, TimeSpan.FromSeconds(5));

    saved.CreatedBy.Should().Be("system");
  }

  [Fact]
  public async Task SoftDelete_ShouldApplyGlobalFilter()
  {
    Guid id;

    using (var scope = _fixture.CreateScope())
    {
      var db =
          scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

      var factory =
          scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

      var entity = factory.Create();
      entity.Label = $"Disposable {Guid.NewGuid():N}";

      db.Items.Add(entity);
      await db.SaveChangesAsync();

      id = entity.Id;

      db.Items.Remove(entity);
      await db.SaveChangesAsync();

      db.ChangeTracker.Clear();
    }

    using var verifyScope = _fixture.CreateScope();

    var verifyDb =
        verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

    var filtered =
        await verifyDb.Items.FirstOrDefaultAsync(x => x.Id == id);

    var raw =
        await verifyDb.Items
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id);

    filtered.Should().BeNull();

    raw.Should().NotBeNull();
    raw!.IsDeleted.Should().BeTrue();
  }

  [Fact]
  public async Task GlobalFilter_ShouldExcludeSoftDeleted()
  {
    using var scope = _fixture.CreateScope();

    var db =
        scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

    var factory =
        scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

    var active = factory.Create();
    active.Label = $"Active {Guid.NewGuid():N}";

    var deleted = factory.Create();
    deleted.Label = $"Deleted {Guid.NewGuid():N}";

    db.Items.AddRange(active, deleted);
    await db.SaveChangesAsync();

    db.Items.Remove(deleted);
    await db.SaveChangesAsync();

    db.ChangeTracker.Clear();

    using var verifyScope = _fixture.CreateScope();

    var verifyDb =
        verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

    var visible =
        await verifyDb.Items.ToListAsync();

    visible.Should()
        .ContainSingle(x => x.Id == active.Id);

    var all =
        await verifyDb.Items.IgnoreQueryFilters().ToListAsync();

    all.Should().HaveCount(2);

    all.Should()
        .Contain(x =>
            x.Id == deleted.Id &&
            x.IsDeleted);
  }
}