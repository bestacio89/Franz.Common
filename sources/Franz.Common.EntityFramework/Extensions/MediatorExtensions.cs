using Franz.Common.Business.Domain;
using Microsoft.EntityFrameworkCore;

namespace MediatR;
internal static class MediatorExtensions
{
  public static async Task DispatchDomainEventsAsync(this IMediator mediator, DbContext dbContext, CancellationToken cancellationToken = default)
  {
    var domainEntities = dbContext.ChangeTracker
        .Entries<Entity>()
        .Where(x => x.Entity.Events.Any())
        .ToList();

    var domainEvents = domainEntities
        .SelectMany(x => x.Entity.Events)
        .ToList();

    domainEntities
        .ForEach(entity => entity.Entity.ClearEvents());

    foreach (var domainEvent in domainEvents)
      await mediator.Publish(domainEvent, cancellationToken);
  }
}
