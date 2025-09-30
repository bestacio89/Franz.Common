using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Mappings.Contracts.Mappers;
public interface IArasAggregateMapper<TAggregate, TEvent>
        where TAggregate : AggregateRoot<TEvent>
        where TEvent : IDomainEvent
{
  /// <summary>
  /// Build an aggregate from ARAS persisted state.
  /// </summary>
  TAggregate MapFromState(IDictionary<string, object> arasData);

  /// <summary>
  /// Build an aggregate by replaying events.
  /// </summary>
  TAggregate MapFromEvents(IEnumerable<TEvent> events);

  /// <summary>
  /// Extract state/events back to ARAS format.
  /// </summary>
  IDictionary<string, object> MapToAras(TAggregate aggregate);
}
