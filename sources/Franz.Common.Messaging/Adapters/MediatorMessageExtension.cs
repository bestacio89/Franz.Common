using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using System.Text.Json;

namespace Franz.Common.Messaging.Adapters;

public static class MediatorMessageExtensions
{
  // Hardened options for 1.7.8 to ensure records and init-only props serialize correctly
  private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

  public static Message ToMessage(this ICommand command)
  {
    // Use the options here!
    var json = JsonSerializer.Serialize((object)command, _options);
    var msg = new Message(json);

    msg.MessageType = command.GetType().FullName;
    msg.CorrelationId = Guid.CreateVersion7(); // confirmed: GuidV7 is working!
    msg.SetProperty("CommandType", command.GetType().Name);
    return msg;
  }

  public static Message ToMessage(this IEvent @event)
  {
    // Use the options here!
    var json = JsonSerializer.Serialize((object)@event, _options);
    var msg = new Message(json);

    msg.MessageType = @event.GetType().FullName;
    msg.CorrelationId = Guid.CreateVersion7();
    msg.SetProperty("EventType", @event.GetType().Name);
    return msg;
  }
}