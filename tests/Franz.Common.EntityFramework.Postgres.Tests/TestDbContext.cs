using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public class TestDbContext(
    DbContextOptions<TestDbContext> options,
    IDispatcher dispatcher,
    ICurrentUserService? currentUser = null,
    ITenantExecutionContextAccessor? tenantContextAccessor = null)
    : DbContextBase(options, dispatcher, currentUser, tenantContextAccessor)
{
  public DbSet<TestEntity> TestEntities => Set<TestEntity>();
  public DbSet<TenantOwnedTestEntity> TenantOwnedEntities => Set<TenantOwnedTestEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
  }
}

public sealed class TenantOwnedTestEntity : Franz.Common.Business.Domain.Entity<Guid>, ITenantEntity
{
  public Guid TenantId { get; set; }
  public string Name { get; set; } = string.Empty;
}
