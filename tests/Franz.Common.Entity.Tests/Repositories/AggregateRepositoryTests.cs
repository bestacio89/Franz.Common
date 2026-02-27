using Franz.Common.EntityFramework.Tests.Extensions.Dummies;
using Franz.Common.EntityFramework.Tests.Repositories.Fakes;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Franz.Common.EntityFramework.Tests.Repositories;

public class AggregateRepositoryTests
{
  [Fact]
  public async Task SaveAsync_PersistsAndDispatchesEvents()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var dispatcherMock = new Mock<IDispatcher>();
    using var context = new TestDbContext3(options, dispatcherMock.Object);
    var repo = new DummyAggregateRepository(context, dispatcherMock.Object);

    var aggregate = new DummyAggregate();
    aggregate.DoSomething(); // raises one event

    // Act
    await repo.SaveAsync(aggregate);

    // Assert
    var eventsInDb = await context.Set<DummyEvent>().ToListAsync();
    Assert.Single(eventsInDb); // persisted to DB
    dispatcherMock.Verify(d => d.PublishEventAsync(It.IsAny<DummyEvent>(), default), Times.Once);
    Assert.Empty(aggregate.GetUncommittedChanges()); // changes cleared
  }

  [Fact]
  public async Task GetByIdAsync_RehydratesAggregateFromEvents()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var dispatcherMock = new Mock<IDispatcher>();
    using var context = new TestDbContext3(options, dispatcherMock.Object);

    var aggregateId = Guid.NewGuid();
    var ev = new DummyEvent { AggregateId = aggregateId };
    await context.Set<DummyEvent>().AddAsync(ev);
    await context.SaveChangesAsync();

    var repo = new DummyAggregateRepository(context, dispatcherMock.Object);

    // Act
    var aggregate = await repo.GetByIdAsync(aggregateId);

    // Assert
    Assert.NotNull(aggregate);
    Assert.Equal(aggregateId, aggregate.Id);
  }

  [Fact]
  public async Task GetByIdAsync_ThrowsNotFound_WhenNoEvents()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var dispatcherMock = new Mock<IDispatcher>();
    using var context = new TestDbContext3(options, dispatcherMock.Object);
    var repo = new DummyAggregateRepository(context, dispatcherMock.Object);

    // Act & Assert
    await Assert.ThrowsAsync<Franz.Common.Errors.NotFoundException>(
        async () => await repo.GetByIdAsync(Guid.NewGuid())
    );
  }
}