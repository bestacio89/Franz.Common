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
      CancellationToken cancellationToken = default
  )
  {
    ApplyAuditing();

    var result = await base.SaveChangesAsync(cancellationToken);

    await DispatchDomainEventsAsync();

    return result;
  }

  private void ApplyAuditing()
  {
    var userId = _currentUser?.UserId ?? "system";

    foreach (var entry in ChangeTracker.Entries<Entity>())
    {
      switch (entry.State)
      {
        case EntityState.Added:
          entry.Entity.MarkCreated(userId);
          break;

        case EntityState.Modified:
          entry.Entity.MarkUpdated(userId);
          break;

        case EntityState.Deleted:
          entry.Entity.MarkDeleted(userId);
          // Ensure soft delete is persisted
          entry.State = EntityState.Modified;
          break;
      }
    }
  }

  private async Task DispatchDomainEventsAsync()
  {
    var domainEntities = ChangeTracker.Entries<Entity>()
        .Where(x => x.Entity.Events.Any())
        .Select(x => x.Entity)
        .ToList();

    foreach (var entity in domainEntities)
    {
      var events = entity.Events.ToList();
      entity.ClearEvents();

      foreach (var domainEvent in events)
      {
        await _dispatcher.PublishAsync(domainEvent);
      }
    }
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
      {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var isDeletedProperty = Expression.Property(parameter, nameof(Entity.IsDeleted));
        var filter = Expression.Lambda(
            Expression.Equal(isDeletedProperty, Expression.Constant(false)),
            parameter
        );

        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
      }
    }
  }
}
