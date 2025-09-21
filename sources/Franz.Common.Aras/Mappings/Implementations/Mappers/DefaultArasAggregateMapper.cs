using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Mappings.Implementations.Mappers;
public class DefaultArasAggregateMapper<TAggregate, TEvent> : IArasAggregateMapper<TAggregate, TEvent>
        where TAggregate : AggregateRoot<TEvent>, new()
        where TEvent : BaseDomainEvent
{
  private readonly IArasEntityMapper<TAggregate> _entityMapper;

  public DefaultArasAggregateMapper(IArasEntityMapper<TAggregate> entityMapper)
  {
    _entityMapper = entityMapper;
  }

  public TAggregate MapFromState(IDictionary<string, object> arasData)
  {
    // Hydrate aggregate directly from ARAS fields
    return _entityMapper.MapFromAras(arasData);
  }

  public TAggregate MapFromEvents(IEnumerable<TEvent> events)
  {
    var aggregate = new TAggregate();
    aggregate.ReplayEvents(events);
    return aggregate;
  }

  public IDictionary<string, object> MapToAras(TAggregate aggregate)
  {
    // Persist current state only (could also push uncommitted events to ARAS history table)
    return _entityMapper.MapToAras(aggregate);
  }
}