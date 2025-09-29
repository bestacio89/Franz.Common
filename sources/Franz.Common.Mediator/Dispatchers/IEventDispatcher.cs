using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Dispatchers;
public interface IEventDispatcher
{
  Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
      where TEvent : IEvent;
}
