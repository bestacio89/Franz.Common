using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.EntityFramework.ProviderContract.Tests;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.MySql.Tests;

public sealed class TestDbContext(
    DbContextOptions<TestDbContext> options,
    IDispatcher dispatcher,
    ICurrentUserService? currentUser = null,
    ITenantExecutionContextAccessor? tenantContextAccessor = null)
    : DbContextBase(options, dispatcher, currentUser, tenantContextAccessor)
{
  public DbSet<TestEntity> Entities => Set<TestEntity>();
  public DbSet<TestChild> Children => Set<TestChild>();
  public DbSet<TenantOwnedTestEntity> TenantOwnedEntities => Set<TenantOwnedTestEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<TestEntity>(entity =>
    {
      entity.ToTable("provider_test_entities");
      entity.HasKey(x => x.Id);
      entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
      entity.HasMany(x => x.Children)
          .WithOne(x => x.Parent)
          .HasForeignKey(x => x.ParentId)
          .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<TestChild>(entity =>
    {
      entity.ToTable("provider_test_children");
      entity.HasKey(x => x.Id);
      entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
    });
  }
}

public sealed class TestEntity : Entity<Guid>
{
  public string Name { get; set; } = string.Empty;
  public List<TestChild> Children { get; set; } = [];
}

public sealed class TestChild : Entity<Guid>
{
  public string Name { get; set; } = string.Empty;
  public Guid ParentId { get; set; }
  public TestEntity Parent { get; set; } = null!;
}
