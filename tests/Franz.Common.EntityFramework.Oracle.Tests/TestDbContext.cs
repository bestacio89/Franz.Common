using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.EntityFramework.ProviderContract.Tests;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Franz.Common.EntityFramework.Oracle.Tests;

public class TestDbContext(
    DbContextOptions<TestDbContext> options,
    IDispatcher dispatcher,
    ICurrentUserService? currentUser = null,
    ITenantExecutionContextAccessor? tenantContextAccessor = null,
    IOptions<MultiTenancyOptions>? multiTenancyOptions = null)
    : DbContextBase(
        options,
        dispatcher,
        currentUser,
        tenantContextAccessor,
        databaseOptions: null,
        multiTenancyOptions ?? Options.Create(new MultiTenancyOptions { Enabled = true }))
{
  public DbSet<TestEntity> TestEntities => Set<TestEntity>();
  public DbSet<TenantOwnedTestEntity> TenantOwnedEntities => Set<TenantOwnedTestEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<TenantOwnedTestEntity>().ToTable("provider_contract_tenant_owned_entities");
  }
}
