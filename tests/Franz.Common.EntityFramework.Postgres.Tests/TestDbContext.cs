using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public class TestDbContext : DbContextBase
{
  public TestDbContext(
      DbContextOptions<TestDbContext> options,
      IDispatcher dispatcher,
      ICurrentUserService? currentUser = null)
      : base(options, dispatcher, currentUser)
  {
  }
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      // FIX: Check against IEntity interface
      if (typeof(IEntity).IsAssignableFrom(entityType.ClrType))
      {
        var parameter = Expression.Parameter(entityType.ClrType, "e");

        // FIX: Access "IsDeleted" by string name since it's on the generic base
        var isDeletedProperty = Expression.Property(parameter, "IsDeleted");

        var filter = Expression.Lambda(
            Expression.Equal(isDeletedProperty, Expression.Constant(false)),
            parameter);

        modelBuilder
          .Entity(entityType.ClrType)
          .HasQueryFilter(filter);
      }
    }
  }
  public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}