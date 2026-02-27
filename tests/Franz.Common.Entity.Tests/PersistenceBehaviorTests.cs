using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.EntityFramework.Behaviors;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Franz.Common.EntityFramework.Tests.Extensions.Dummies;

namespace Franz.Common.EntityFramework.Tests
{
  // ------------------------------
  // Test DbContext for DummyEntity2
  // ------------------------------
  public class TestDbContext4 : DbContextBase
  {
    public TestDbContext4(
        DbContextOptions<TestDbContext4> options,
        IDispatcher dispatcher,
        ICurrentUserService? currentUser = null
    ) : base(options, dispatcher, currentUser)
    { }

    public DbSet<DummyEntity2> DummyEntities2 => Set<DummyEntity2>();
  }

  // ------------------------------
  // Dummy Command for pipeline tests
  // ------------------------------
  public class TestCommand : Franz.Common.Mediator.Messages.ICommand<string> { }

  // ------------------------------
  // The full test class
  // ------------------------------
  public class PersistenceBehaviorAndDbContextTests
  {
    private readonly IDispatcher _dispatcher;
    private readonly Mock<ICurrentUserService> _mockUser;

    public PersistenceBehaviorAndDbContextTests()
    {
      // Minimal ServiceProvider for FranzDispatcher
      var services = new ServiceCollection();
      services.AddScoped<IDispatcher, FranzDispatcher>();
      var serviceProvider = services.BuildServiceProvider();

      _dispatcher = new FranzDispatcher(serviceProvider);

      // Mock current user
      _mockUser = new Mock<ICurrentUserService>();
      _mockUser.Setup(u => u.UserId).Returns("test-user");
    }

    [Fact]
    public async Task DbContextBase_AppliesAuditing_OnSaveChanges()
    {
      var options = new DbContextOptionsBuilder<TestDbContext4>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      await using var db = new TestDbContext4(options, _dispatcher, _mockUser.Object);

      var entity = new DummyEntity2() { 
        Name = "alpha"};
      
      await db.DummyEntities2.AddAsync(entity);
      await db.SaveChangesAsync();

      Assert.Equal("test-user", entity.CreatedBy);
      Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task PersistenceBehavior_Handle_CallsNext_LogsAndSavesChanges()
    {
      var options = new DbContextOptionsBuilder<TestDbContext4>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      // Use a real DbContext instance with in-memory DB
      await using var db = new TestDbContext4(options, _dispatcher, _mockUser.Object);

      // Mock Logger
      var mockLogger = new Mock<ILogger<PersistenceBehavior<TestCommand, string>>>();

      var behavior = new PersistenceBehavior<TestCommand, string>(db, mockLogger.Object);

      bool nextCalled = false;
      Func<Task<string>> next = () =>
      {
        nextCalled = true;
        return Task.FromResult("response");
      };

      var command = new TestCommand();

      // Act
      var result = await behavior.Handle(command, next, CancellationToken.None);

      // Assert
      Assert.True(nextCalled);
      Assert.Equal("response", result);

      // Check that SaveChangesAsync persisted something (even if empty)
      await db.SaveChangesAsync(); // Should not throw

      // Verify logging happened
      mockLogger.Verify(
          x => x.Log(
              LogLevel.Information,
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => true),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.AtLeast(2)
      );
    }
  }
}