using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Events.PostProcessing;
public interface IEventPostProcessor<TEvent> where TEvent : IEvent
{
  Task ProcessAsync(TEvent @event, CancellationToken ct = default);
}
