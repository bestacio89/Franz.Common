using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Adapters;
public static class MediatorMessageExtensions
{
  public static Message ToMessage(this ICommand command)
  {
    var msg = new Message(JsonSerializer.Serialize(command));
    msg.MessageType = command.GetType().FullName;
    msg.CorrelationId = Guid.NewGuid().ToString();
    msg.SetProperty("CommandType", command.GetType().Name);
    return msg;
  }

  public static Message ToMessage(this IEvent @event)
  {
    var msg = new Message(JsonSerializer.Serialize(@event));
    msg.MessageType = @event.GetType().FullName;
    msg.CorrelationId = Guid.NewGuid().ToString();
    msg.SetProperty("EventType", @event.GetType().Name);
    return msg;
  }
}