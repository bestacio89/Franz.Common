using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework;

public abstract class DbContextBase : DbContext
{
  private readonly IDispatcher _dispatcher;
  private readonly ICurrentUserService? _currentUser;

  protected DbContextBase(
      DbContextOptions options,
      IDispatcher dispatcher,
      ICurrentUserService? currentUser = null
  ) : base(options)
  {
    _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    _currentUser = currentUser;
  }

  public override async Task<int> SaveChangesAsync(
      CancellationToken cancellationToken = default)
  {
    ApplyAuditing();

    var result = await base.SaveChangesAsync(cancellationToken);


    return result;
  }

  private void ApplyAuditing()
  {
    var userId = _currentUser?.UserId ?? "system";

    foreach (var entry in ChangeTracker.Entries())
    {
      if (entry.Entity is not IEntity<object> &&
          !ImplementsIEntity(entry.Entity.GetType()))
      {
        continue;
      }

      switch (entry.State)
      {
        case EntityState.Added:
          ((dynamic)entry.Entity).MarkCreated(userId);
          break;

        case EntityState.Modified:
          ((dynamic)entry.Entity).MarkUpdated(userId);
          break;

        case EntityState.Deleted:
          ((dynamic)entry.Entity).MarkDeleted(userId);

          entry.State = EntityState.Modified;
          break;
      }
    }
  }

  private static bool ImplementsIEntity(Type type)
  {
    return type.GetInterfaces()
        .Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntity<>));
  }
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      var clrType = entityType.ClrType;

      if (!ImplementsIEntity(clrType))
        continue;

      var parameter = Expression.Parameter(clrType, "e");
      var isDeleted = Expression.Property(parameter, nameof(IEntity<object>.IsDeleted));

      var filter = Expression.Lambda(
        Expression.Equal(isDeleted, Expression.Constant(false)),
        parameter);

      modelBuilder
        .Entity(clrType)
        .HasQueryFilter(filter);
    }
  }
}