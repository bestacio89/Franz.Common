using System.Diagnostics.CodeAnalysis;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Adapters;

public static class MediatorMessageExtensions
{
  [RequiresUnreferencedCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  [RequiresDynamicCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  public static Message ToMessage(this ICommand command)
  {
    ArgumentNullException.ThrowIfNull(command);
    return ((ICommandMessage)command).ToMessage(new JsonMessageSerializer());
  }

  [RequiresUnreferencedCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  [RequiresDynamicCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  public static Message ToMessage(this ICommandMessage command)
  {
    ArgumentNullException.ThrowIfNull(command);
    return command.ToMessage(new JsonMessageSerializer());
  }

  /// <summary>Creates a command envelope using the application's configured serializer and generated metadata.</summary>
  public static Message ToMessage(this ICommandMessage command, IMessageSerializer serializer)
  {
    ArgumentNullException.ThrowIfNull(command);
    ArgumentNullException.ThrowIfNull(serializer);

    var type = command.GetType();
    var message = new Message(serializer.Serialize(command, type))
    {
      MessageType = type.FullName,
      CorrelationId = Guid.CreateVersion7()
    };

    if (command is ICorrelatedMessage correlated)
      correlated.CorrelationId = message.CorrelationId;

    message.SetProperty("CommandType", type.Name);
    return message;
  }

  [RequiresUnreferencedCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  [RequiresDynamicCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  public static Message ToMessage(this IEvent @event)
  {
    ArgumentNullException.ThrowIfNull(@event);
    return @event.ToMessage(new JsonMessageSerializer());
  }

  /// <summary>Creates an event envelope using the application's configured serializer and generated metadata.</summary>
  public static Message ToMessage(this IEvent @event, IMessageSerializer serializer)
  {
    ArgumentNullException.ThrowIfNull(@event);
    ArgumentNullException.ThrowIfNull(serializer);

    var type = @event.GetType();
    var message = new Message(serializer.Serialize(@event, type))
    {
      MessageType = type.FullName,
      CorrelationId = Guid.CreateVersion7()
    };

    if (@event is ICorrelatedMessage correlated)
      correlated.CorrelationId = message.CorrelationId;

    message.SetProperty("EventType", type.Name);
    return message;
  }
}
