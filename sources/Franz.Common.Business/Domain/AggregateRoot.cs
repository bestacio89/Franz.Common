using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

public abstract class AggregateRoot<TEvent> : Entity<Guid>, IAggregateRoot
    where TEvent : BaseDomainEvent
{
  private readonly List<TEvent> _changes = new();
  public int Version { get; private set; } = -1;

  protected AggregateRoot() { }

  protected AggregateRoot(Guid id) : base()
  {
    Id = id;
  }

  public IEnumerable<TEvent> GetUncommittedChanges() => _changes.AsReadOnly();

  public void MarkChangesAsCommitted() => _changes.Clear();

  private void ApplyChange(TEvent @event, bool isNew)
  {
    var method = GetType().GetMethod("Apply", new[] { @event.GetType() });

    if (method == null)
      throw new InvalidOperationException($"Apply method not found for {@event.GetType().Name}.");

    method.Invoke(this, new object[] { @event });

    if (isNew)
    {
      _changes.Add(@event);
    }
  }

  protected void RaiseEvent(TEvent @event)
  {
    ApplyChange(@event, true);
  }

  public void ReplayEvents(IEnumerable<TEvent> events)
  {
    foreach (var @event in events)
    {
      ApplyChange(@event, false);
    }
  }
}
