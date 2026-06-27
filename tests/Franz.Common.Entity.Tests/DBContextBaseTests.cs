using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Business.Domain;
using System.Linq;
using System.Threading.Tasks;

namespace Franz.Common.EntityFramework.Tests
{
  // =========================
  // Dummy Entity
  // =========================
  public class DummyEntity2 : Entity<int>
  {
    public string Name { get; set; } = default!;
  }

  // =========================
  // Test DbContext inheriting from DbContextBase
  // =========================
  public class TestDbContext : DbContextBase
  {
    public TestDbContext(DbContextOptions<TestDbContext> options, IDispatcher dispatcher, ICurrentUserService? user = null)
        : base(options, dispatcher, user)
    { }

    public DbSet<DummyEntity2> DummyEntities => Set<DummyEntity2>();
  }

  // =========================
  // Unit Tests
  // =========================
  public class DbContextBaseTests
  {
    private TestDbContext CreateDbContext(Mock<IDispatcher>? dispatcher = null, ICurrentUserService? user = null)
    {
      var options = new DbContextOptionsBuilder<TestDbContext>()
          .UseInMemoryDatabase("TestDb_" + System.Guid.NewGuid())
          .Options;

      dispatcher ??= new Mock<IDispatcher>();
      return new TestDbContext(options, dispatcher.Object, user);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldMarkCreated_WithUserId()
    {
      var mockUser = new Mock<ICurrentUserService>();
      mockUser.Setup(u => u.UserId).Returns("test-user");

      var context = CreateDbContext(user: mockUser.Object);
      var entity = new DummyEntity2 { Name = "Test" };
      context.DummyEntities.Add(entity);

      await context.SaveChangesAsync();

      Assert.Equal("test-user", entity.CreatedBy);
      Assert.NotEqual(default, entity.DateCreated);
      Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldMarkUpdated_WithUserId()
    {
      var mockUser = new Mock<ICurrentUserService>();
      mockUser.Setup(u => u.UserId).Returns("test-user");

      var context = CreateDbContext(user: mockUser.Object);
      var entity = new DummyEntity2 { Name = "Test" };
      context.DummyEntities.Add(entity);
      await context.SaveChangesAsync();

      entity.Name = "Updated";
      await context.SaveChangesAsync();

      Assert.Equal("test-user", entity.LastModifiedBy);
      Assert.NotEqual(default, entity.LastModifiedDate);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldMarkDeleted_AndSoftDelete()
    {
      var mockUser = new Mock<ICurrentUserService>();
      mockUser.Setup(u => u.UserId).Returns("test-user");

      var context = CreateDbContext(user: mockUser.Object);
      var entity = new DummyEntity2 { Name = "ToDelete" };
      context.DummyEntities.Add(entity);
      await context.SaveChangesAsync();

      context.DummyEntities.Remove(entity);
      await context.SaveChangesAsync();

      // Soft-delete assertions
      Assert.True(entity.IsDeleted);
      Assert.Equal("test-user", entity.DeletedBy);
  

      // Ensure soft-deleted entities remain in the DB
      var allEntities = context.DummyEntities.IgnoreQueryFilters().ToList();
      Assert.Contains(entity, allEntities);

      // Normal queries respect soft-delete filter
      var activeEntities = context.DummyEntities.ToList();
      Assert.DoesNotContain(entity, activeEntities);
    }
  }
}