using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.Oracle.Tests;

public class TestDbContext : DbContextBase
{
  public TestDbContext(
      DbContextOptions<TestDbContext> options,
      IDispatcher dispatcher,
      ICurrentUserService? currentUser = null)
      : base(options, dispatcher, currentUser)
  {
  }

  // Only domain sets
  public DbSet<TestEntity> TestEntities => Set<TestEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Only test-specific configuration (if any)
    // ❌ DO NOT reapply soft delete logic here anymore
  }
}