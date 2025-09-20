using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Franz.Common.Business
{
  /// <summary>
  /// Base class for all aggregate roots in DDD.
  /// Supports event sourcing by tracking uncommitted domain events,
  /// applying events through registered handlers, and replaying history.
  /// </summary>
  public abstract class AggregateRoot<TEvent> : Entity<Guid>, IAggregateRoot
      where TEvent : BaseDomainEvent
  {
    private readonly List<TEvent> _changes = new();
    private readonly Dictionary<Type, Action<TEvent>> _handlers = new();

    /// <summary>
    /// Version of the aggregate (incremented for every applied event).
    /// </summary>
    public int Version { get; private set; } = -1;

    protected AggregateRoot() { }

    protected AggregateRoot(Guid id) : base()
    {
      Id = id;
    }

    /// <summary>
    /// Get uncommitted changes (events raised since last commit).
    /// </summary>
    public IEnumerable<TEvent> GetUncommittedChanges() => _changes.AsReadOnly();

    /// <summary>
    /// Clear tracked changes after persistence.
    /// </summary>
    public void MarkChangesAsCommitted() => _changes.Clear();

    /// <summary>
    /// Register a handler for a specific event type.
    /// Example: Register&lt;OrderPlaced&gt;(Apply);
    /// </summary>
    protected void Register<T>(Action<T> handler) where T : TEvent
    {
      if (_handlers.ContainsKey(typeof(T)))
        throw new InvalidOperationException($"Handler already registered for {typeof(T).Name}");

      _handlers[typeof(T)] = e => handler((T)e);
    }

    /// <summary>
    /// Raise a new domain event, apply it, and track it as uncommitted.
    /// </summary>
    protected void RaiseEvent(TEvent @event)
    {
      ApplyChange(@event, true);
    }

    /// <summary>
    /// Replay a set of historical events (e.g., from an event store).
    /// These are applied to rebuild state but not tracked as new changes.
    /// </summary>
    public void ReplayEvents(IEnumerable<TEvent> events)
    {
      foreach (var @event in events)
      {
        ApplyChange(@event, false);
      }
    }

    private void ApplyChange(TEvent @event, bool isNew)
    {
      if (!_handlers.TryGetValue(@event.GetType(), out var handler))
        throw new InvalidOperationException($"Apply handler not found for {@event.GetType().Name}");

      handler(@event);
      Version++;

      if (isNew)
        _changes.Add(@event);
    }
  }
}
