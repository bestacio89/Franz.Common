using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.Factories;

public interface IAggregateFactory<TAggregate>
{
  TAggregate Create();
}