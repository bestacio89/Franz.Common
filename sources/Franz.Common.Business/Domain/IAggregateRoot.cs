using Franz.Common.Mediator.Messages;

namespace Franz.Common.Business.Domain;

public interface IAggregateRoot<TEvent>
    where TEvent : IEvent
{
  void Rehydrate(Guid id, IEnumerable<TEvent> events);

  void ReplayEvents(IEnumerable<TEvent> events);

  IReadOnlyCollection<TEvent> GetUncommittedChanges();

  void MarkChangesAsCommitted();
}