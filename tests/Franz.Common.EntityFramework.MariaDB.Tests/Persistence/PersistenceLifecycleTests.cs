using Franz.Common.EntityFramework.MariaDB.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.MariaDB.Tests.Persistence;

[Collection(MariaDbCollection.Name)]
public sealed class PersistenceLifecycleTests(MariaDbFixture database) : MariaDbTestBase(database)
{
  [Fact]
  public async Task CreateUpdateAndRead_PersistsEntityAndAuditMetadata()
  {
    var id = Guid.NewGuid();
    await using (var context = CreateContext())
    {
      await context.Database.EnsureCreatedAsync();
      var entity = new TestEntity { Name = $"create-{id:N}" };
      context.Entities.Add(entity);
      Assert.Equal(1, await context.SaveChangesAsync());
      Assert.NotEqual(Guid.Empty, entity.Id);
      Assert.NotEqual(default, entity.DateCreated);
      Assert.Equal("system", entity.CreatedBy);

      entity.Name = $"updated-{id:N}";
      Assert.Equal(1, await context.SaveChangesAsync());
      Assert.Equal("system", entity.LastModifiedBy);
    }

    await using var verify = CreateContext();
    var persisted = await verify.Entities.SingleAsync(x => x.Name == $"updated-{id:N}");
    Assert.Equal("system", persisted.CreatedBy);
    Assert.Equal("system", persisted.LastModifiedBy);
    Assert.NotEqual(default, persisted.LastModifiedDate);
  }

  [Fact]
  public async Task Remove_SoftDeletesEntityAndHidesItFromDefaultQueries()
  {
    var marker = $"delete-{Guid.NewGuid():N}";
    Guid id;
    await using (var context = CreateContext())
    {
      await context.Database.EnsureCreatedAsync();
      var entity = new TestEntity { Name = marker };
      context.Entities.Add(entity);
      await context.SaveChangesAsync();
      id = entity.Id;

      context.Entities.Remove(entity);
      Assert.Equal(1, await context.SaveChangesAsync());
      Assert.True(entity.IsDeleted);
      Assert.Equal("system", entity.DeletedBy);
      Assert.NotEqual(default, entity.DateDeleted);
    }

    await using var verify = CreateContext();
    Assert.False(await verify.Entities.AnyAsync(x => x.Id == id));
    var retained = await verify.Entities.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
    Assert.True(retained.IsDeleted);
  }


  [Fact]
  public async Task RemoveParent_SoftDeletesTrackedChildrenWithoutPhysicalCascade()
  {
    var marker = $"cascade-{Guid.NewGuid():N}";
    Guid parentId;
    Guid childId;
    await using (var context = CreateContext())
    {
      await context.Database.EnsureCreatedAsync();
      var parent = new TestEntity
      {
        Name = marker,
        Children = [new TestChild { Name = "cascade-child" }]
      };
      context.Entities.Add(parent);
      await context.SaveChangesAsync();
      parentId = parent.Id;
      childId = parent.Children.Single().Id;

      context.Entities.Remove(parent);
      await context.SaveChangesAsync();
    }

    await using var verify = CreateContext();
    Assert.False(await verify.Entities.AnyAsync(x => x.Id == parentId));
    Assert.False(await verify.Children.AnyAsync(x => x.Id == childId));
    Assert.True(await verify.Entities.IgnoreQueryFilters().AnyAsync(x => x.Id == parentId && x.IsDeleted));
    Assert.True(await verify.Children.IgnoreQueryFilters().AnyAsync(x => x.Id == childId && x.IsDeleted));
  }

  [Fact]
  public async Task SaveChanges_PersistsParentAndChildrenAndTranslatesQueries()
  {
    var marker = $"parent-{Guid.NewGuid():N}";
    await using (var context = CreateContext())
    {
      await context.Database.EnsureCreatedAsync();
      var parent = new TestEntity
      {
        Name = marker,
        Children =
        [
          new TestChild { Name = "child-a" },
          new TestChild { Name = "child-b" }
        ]
      };
      context.Entities.Add(parent);
      Assert.Equal(3, await context.SaveChangesAsync());
    }

    await using var verify = CreateContext();
    var result = await verify.Entities
        .Where(x => x.Name.Contains("parent-") && x.Children.Any(c => c.Name == "child-a"))
        .Select(x => new { x.Id, Count = x.Children.Count })
        .SingleAsync(x => x.Count == 2);

    var loaded = await verify.Entities.Include(x => x.Children).SingleAsync(x => x.Id == result.Id);
    Assert.Equal(2, loaded.Children.Count);
  }
}
