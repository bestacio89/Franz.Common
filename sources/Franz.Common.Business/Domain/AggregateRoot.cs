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

  protected AggregateRoot() { }

  protected AggregateRoot(Guid id)
  {
    SetId(id);
  }

  public IReadOnlyCollection<TEvent> GetUncommittedChanges()
      => _changes.AsReadOnly();

  public void MarkChangesAsCommitted()
      => _changes.Clear();

  protected void Register<T>(Action<T> handler)
      where T : TEvent
  {
    var type = typeof(T);

    if (_handlers.ContainsKey(type))
      throw new InvalidOperationException(
          $"Handler already registered for {type.Name}");

    _handlers[type] = e => handler((T)e);
  }

  public void Rehydrate(Guid id, IEnumerable<TEvent> events)
  {
    SetId(id);

    foreach (var @event in events)
      ApplyChange(@event, false);

    MarkChangesAsCommitted();
  }

  public void ReplayEvents(IEnumerable<TEvent> events)
  {
    foreach (var @event in events)
      ApplyChange(@event, false);
  }

  protected void RaiseEvent(TEvent @event)
      => ApplyChange(@event, true);

  private void ApplyChange(TEvent @event, bool isNew)
  {
    var eventType = @event.GetType();

    if (!_handlers.TryGetValue(eventType, out var handler))
      throw new InvalidOperationException(
          $"Apply handler not found for {eventType.Name}");

    handler(@event);

    Version++;

    if (isNew)
      _changes.Add(@event);
  }
}