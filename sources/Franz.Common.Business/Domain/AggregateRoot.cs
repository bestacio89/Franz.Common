using Franz.Common.Business.Events;

namespace Franz.Common.Business.Domain;
public abstract class AggregateRoot <TEvent>: IAggregateRoot where TEvent: BaseEvent
{
  #region Properties
  public Guid Id { get; protected set; }
  private readonly List<TEvent> _changes = new List<TEvent>();
  public int Version { get; set; } = -1;

  #endregion

  #region Methods
  public IEnumerable<TEvent> getUncomittedchanges()
  {
    return _changes;
  }
  public void MarkChangesAsCommited()
  {
    _changes.Clear();
  }
  private void ApplyChange(TEvent @event, bool isNew)
  {
    var method = this.GetType().GetMethod("Apply", new Type[] { @event.GetType() });
    if (method == null)
    {
      throw new ArgumentNullException(nameof(@event), $"The apply method was not found in the aggregate for {@event.GetType().Name}!");
    }
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

  public void ReplayEvents(IEnumerable<TEvent> events, DateTime? startDate = null, DateTime? endDate = null)
  {
    foreach (var @event in events)
    {
      if ((startDate == null || @event.Date >= startDate.Value)
          && (endDate == null || @event.Date <= endDate.Value))
      {
        ApplyChange(@event, false);
      }
    }
  }
  public void ReplayEvents(IEnumerable<TEvent> events, DateTime? date = null)
  {
    foreach (var @event in events)
    {
      if (date == null || @event.Date <= date.Value)
      {
        ApplyChange(@event, false);
      }
    }
  }
  #endregion
}

