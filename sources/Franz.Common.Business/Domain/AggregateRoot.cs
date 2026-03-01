#nullable enable

using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;

namespace Franz.Common.Business.Domain;

public abstract class AggregateRoot<TEvent> : Entity<Guid>, IAggregateRoot<TEvent>
    where TEvent : IEvent
{
  private readonly List<TEvent> _changes = new();
  private readonly Dictionary<Type, Action<TEvent>> _handlers = new();

  public int Version { get; private set; } = -1;

  protected AggregateRoot()
  {
    // Hardening: Initialize with Guid v7 for sequential DB inserts
    Id = Guid.CreateVersion7();
  }

  protected AggregateRoot(Guid id) : base()
  {
    // Respect the provided ID, but fallback to v7 if it's empty
    Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
  }

  public IReadOnlyCollection<TEvent> GetUncommittedChanges() => _changes.AsReadOnly();

  public void MarkChangesAsCommitted() => _changes.Clear();

  protected void Register<T>(Action<T> handler) where T : TEvent
  {
    if (_handlers.ContainsKey(typeof(T)))
      throw new InvalidOperationException($"Handler already registered for {typeof(T).Name}");

    _handlers[typeof(T)] = e => handler((T)e);
  }

  protected void RaiseEvent(TEvent @event)
  {
    // We apply the change first to update internal state
    ApplyChange(@event, true);
  }

  public void ReplayEvents(IEnumerable<TEvent> events)
  {
    foreach (var @event in events)
    {
      ApplyChange(@event, false);
    }
  }

  public void Rehydrate(Guid id, IEnumerable<TEvent> events)
  {
    Id = id;
    ReplayEvents(events);
    MarkChangesAsCommitted();
  }

  private void ApplyChange(TEvent @event, bool isNew)
  {
    var eventType = @event.GetType();
    if (!_handlers.TryGetValue(eventType, out var handler))
      throw new InvalidOperationException($"Apply handler not found for {eventType.Name}");

    handler(@event);
    Version++;

    if (isNew)
      _changes.Add(@event);
  }
}