using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Handlers;
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
  Task HandleAsync(TEvent @event, CancellationToken ct = default);
}

