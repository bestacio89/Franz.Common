using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Franz.Common.EntityFramework.ProviderContract.Tests;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.Postgres.Tests;

using ProviderTenantOwnedTestEntity = Franz.Common.EntityFramework.ProviderContract.Tests.TenantOwnedTestEntity;

public class TestDbContext(
    DbContextOptions<TestDbContext> options,
    IDispatcher dispatcher,
    ICurrentUserService? currentUser = null,
    ITenantExecutionContextAccessor? tenantContextAccessor = null)
    : DbContextBase(options, dispatcher, currentUser, tenantContextAccessor)
{
  public DbSet<TestEntity> TestEntities => Set<TestEntity>();
  public DbSet<TenantOwnedTestEntity> TenantOwnedEntities => Set<TenantOwnedTestEntity>();
  public DbSet<ProviderTenantOwnedTestEntity> ProviderContractTenantOwnedEntities => Set<ProviderTenantOwnedTestEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<ProviderTenantOwnedTestEntity>().ToTable("provider_contract_tenant_owned_entities");
  }
}

public sealed class TenantOwnedTestEntity : Franz.Common.Business.Domain.Entity<Guid>, ITenantEntity
{
  public Guid TenantId { get; set; }
  public string Name { get; set; } = string.Empty;
}
