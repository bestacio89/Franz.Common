using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Validation.Events;
public interface IEventPipeline<TEvent> where TEvent : IEvent
{
  Task HandleAsync(TEvent @event, Func<Task> next, CancellationToken ct = default);
}