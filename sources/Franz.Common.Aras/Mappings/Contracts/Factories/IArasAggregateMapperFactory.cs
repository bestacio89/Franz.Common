using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Mappings.Contracts.Factories;
/// <summary>
/// Factory to resolve aggregate mappers via DI.
/// </summary>
public interface IArasAggregateMapperFactory
{
  IArasAggregateMapper<TAggregate, TEvent> Resolve<TAggregate, TEvent>()
      where TAggregate : AggregateRoot<TEvent>
      where TEvent : BaseDomainEvent;
}