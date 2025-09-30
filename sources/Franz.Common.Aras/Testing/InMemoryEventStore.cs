using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

namespace Franz.Common.Aras.Testing
{
  public class InMemoryEventStore
  {
    private readonly Dictionary<Guid, List<IDomainEvent>> _events = new();

    public void Append(Guid aggregateId, IEnumerable<IDomainEvent> events)
    {
      if (!_events.ContainsKey(aggregateId))
        _events[aggregateId] = new List<IDomainEvent>();

      _events[aggregateId].AddRange(events);
    }

    public IReadOnlyList<IDomainEvent> Load(Guid aggregateId)
    {
      return _events.TryGetValue(aggregateId, out var evts)
          ? evts
          : Array.Empty<IDomainEvent>();
    }
  }
}
