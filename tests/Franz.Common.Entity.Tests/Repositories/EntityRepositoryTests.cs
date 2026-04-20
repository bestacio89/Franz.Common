using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.EntityFramework.Tests.Repositories.Fakes;
using Franz.Common.Errors;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Franz.Common.EntityFramework.Tests.Repositories;

public class EntityRepositoryTests
{
  private static TestDbContext3 CreateDbContext()
  {
    var options =
        new DbContextOptionsBuilder<TestDbContext3>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    return new TestDbContext3(
        options,
        new Mock<IDispatcher>().Object);
  }

  private static DummyEntity CreateEntity()
  {
    var factory =
        new EntityFactory<Guid, DummyEntity>(
            new GuidV7Generator());

    return factory.Create();
  }

  [Fact]
  public async Task AddAsync_ShouldPersistEntity()
  {
    using var context = CreateDbContext();

    var repo =
        new EntityRepository<
            TestDbContext3,
            DummyEntity,
            Guid>(context);

    var entity = CreateEntity();

    await repo.AddAsync(entity);

    var persisted =
        await context.Set<DummyEntity>()
            .FirstOrDefaultAsync(x => x.Id == entity.Id);

    Assert.NotNull(persisted);
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnEntity()
  {
    using var context = CreateDbContext();

    var repo =
        new EntityRepository<
            TestDbContext3,
            DummyEntity,
            Guid>(context);

    var entity = CreateEntity();

    await repo.AddAsync(entity);

    var fetched =
        await repo.GetByIdAsync(entity.Id);

    Assert.NotNull(fetched);
    Assert.Equal(entity.Id, fetched!.Id);
  }

  [Fact]
  public async Task UpdateAsync_ShouldPersistChanges()
  {
    using var context = CreateDbContext();

    var repo =
        new EntityRepository<
            TestDbContext3,
            DummyEntity,
            Guid>(context);

    var entity = CreateEntity();

    await repo.AddAsync(entity);

    entity.SetName("Updated");

    await repo.UpdateAsync(entity);

    var updated =
        await context.Set<DummyEntity>()
            .FirstAsync(x => x.Id == entity.Id);

    Assert.Equal("Updated", updated.Name);
  }

  [Fact]
  public async Task DeleteAsync_ShouldSoftDelete_AndBeNotFoundInNewContext()
  {
    // Arrange
    var entity = CreateEntity();
    using (var context = CreateDbContext())
    {
      var repo = new EntityRepository<TestDbContext3, DummyEntity, Guid>(context);
      await repo.AddAsync(entity);
      await repo.DeleteAsync(entity);
    } // Context disposed, cache cleared

    // Act & Assert
    using (var context = CreateDbContext()) // New context, no local cache
    {
      var repo = new EntityRepository<TestDbContext3, DummyEntity, Guid>(context);

      // NOW it will hit the DB, apply the filter, return null, and throw.
      await repo.Awaiting(r => r.GetByIdAsync(entity.Id))
          .Should().ThrowAsync<NotFoundException>();
    }
  }
}