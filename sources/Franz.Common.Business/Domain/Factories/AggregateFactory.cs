using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.Factories;



public sealed class AggregateFactory<TAggregate, TEvent> : IAggregateFactory<TAggregate>
    where TAggregate : AggregateRoot<TEvent>
    where TEvent : IEvent
{
  private readonly Func<Guid, TAggregate> _activator;
  private readonly IIdGenerator<Guid> _idGenerator;

  public AggregateFactory(
      IIdGenerator<Guid> idGenerator,
      Func<Guid, TAggregate> activator)
  {
    _idGenerator = idGenerator;
    _activator = activator;
  }

  public TAggregate Create()
  {
    var id = _idGenerator.Create();
    return _activator(id);
  }
}