using Franz.Common.Mediator.Messages;

namespace Franz.Common.Business.Domain;

public interface IAggregateRoot<TEvent> where TEvent : IEvent
{
  /// <summary>
  /// Rehydrates the aggregate from its historical events.
  /// </summary>
  void Rehydrate(Guid id, IEnumerable<TEvent> events);

  /// <summary>
  /// Replays a set of historical events (applies without tracking).
  /// </summary>
  void ReplayEvents(IEnumerable<TEvent> events);

  /// <summary>
  /// Gets uncommitted changes raised since the last commit.
  /// </summary>
  IReadOnlyCollection<TEvent> GetUncommittedChanges();

  /// <summary>
  /// Clears uncommitted changes after persistence.
  /// </summary>
  void MarkChangesAsCommitted();
}
