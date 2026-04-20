using Franz.Common.EntityFramework.Tests.Extensions.Dummies;
using Franz.Common.EntityFramework.Tests.Repositories.Fakes;
using Franz.Common.Errors;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Franz.Common.EntityFramework.Tests.Repositories;

public class AggregateRepositoryTests
{
  [Fact]
  public async Task SaveAsync_PersistsAggregate_And_DispatchesEvents()
  {
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var dispatcherMock =
        new Mock<IDispatcher>();

    using var context =
        new TestDbContext3(
            options,
            dispatcherMock.Object);

    var repo =
        new DummyAggregateRepository(
            context,
            dispatcherMock.Object);

    var aggregate =
        new DummyAggregate();

    aggregate.DoSomething();

    // ACT
    await repo.SaveAsync(aggregate);

    // ASSERT

    // Aggregate persisted
    var persisted =
        await context.Set<DummyAggregate>()
            .FirstOrDefaultAsync();

    Assert.NotNull(persisted);

    // Event dispatched
    dispatcherMock.Verify(
        x => x.PublishEventAsync(
            It.IsAny<DummyEvent>(),
            It.IsAny<CancellationToken>()),
        Times.Once);

    // Changes committed
    Assert.Empty(
        aggregate.GetUncommittedChanges());
  }

  [Fact]
  public async Task GetByIdAsync_LoadsPersistedAggregate()
  {
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var dispatcherMock =
        new Mock<IDispatcher>();

    using var context =
        new TestDbContext3(
            options,
            dispatcherMock.Object);

    var aggregate =
        new DummyAggregate();

    context.Add(aggregate);

    await context.SaveChangesAsync();

    var repo =
        new DummyAggregateRepository(
            context,
            dispatcherMock.Object);

    // ACT

    var result =
        await repo.GetByIdAsync(aggregate.Id);

    // ASSERT

    Assert.NotNull(result);

    Assert.Equal(
        aggregate.Id,
        result.Id);
  }

  [Fact]
  public async Task GetByIdAsync_ShouldThrow_WhenAggregateNotFound()
  {
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var dispatcherMock =
        new Mock<IDispatcher>();

    using var context =
        new TestDbContext3(
            options,
            dispatcherMock.Object);

    var repo =
        new DummyAggregateRepository(
            context,
            dispatcherMock.Object);

    await Assert.ThrowsAsync<NotFoundException>(
        () =>
            repo.GetByIdAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task SaveAsync_WithNoEvents_ShouldPersistWithoutDispatching()
  {
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var dispatcherMock =
        new Mock<IDispatcher>();

    using var context =
        new TestDbContext3(
            options,
            dispatcherMock.Object);

    var repo =
        new DummyAggregateRepository(
            context,
            dispatcherMock.Object);

    var aggregate =
        new DummyAggregate();

    // no events raised

    await repo.SaveAsync(aggregate);

    dispatcherMock.Verify(
        x => x.PublishEventAsync(
            It.IsAny<DummyEvent>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
  }
}