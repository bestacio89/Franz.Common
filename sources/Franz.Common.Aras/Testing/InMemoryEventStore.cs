using Franz.Common.Business;
using Franz.Common.Business.Domain;

namespace Franz.Common.Aras.Testing
{
  public class InMemoryEventStore
  {
    private readonly Dictionary<Guid, List<BaseDomainEvent>> _events = new();

    public void Append(Guid aggregateId, IEnumerable<BaseDomainEvent> events)
    {
      if (!_events.ContainsKey(aggregateId))
        _events[aggregateId] = new List<BaseDomainEvent>();

      _events[aggregateId].AddRange(events);
    }

    public IReadOnlyList<BaseDomainEvent> Load(Guid aggregateId)
    {
      return _events.TryGetValue(aggregateId, out var evts)
          ? evts
          : Array.Empty<BaseDomainEvent>();
    }
  }
}
