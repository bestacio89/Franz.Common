using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework;
public static class DomainEventDispatcherExtensions
{
    public static async Task DispatchDomainEventsAsync(
        this IDispatcher dispatcher,
        DbContext context,
        CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await dispatcher.Send(domainEvent, cancellationToken);
    }
}