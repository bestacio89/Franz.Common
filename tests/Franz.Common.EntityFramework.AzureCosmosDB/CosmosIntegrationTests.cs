using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.AzureCosmosDB.Tests;

[Collection("Cosmos")]
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

    var entity = new CosmosEntity
    {
      Label = "Cosmos Architect Item"
    };

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

      var entity = new CosmosEntity
      {
        Label = "Disposable"
      };

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

  
}