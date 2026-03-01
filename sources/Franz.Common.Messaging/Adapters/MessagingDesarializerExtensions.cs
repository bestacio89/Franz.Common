#nullable enable
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Messaging.Messages;
using System.Reflection;
using System.Text.Json;

namespace Franz.Common.Messaging.Adapters;

public static class MessageDeserializerExtensions
{
  private static readonly JsonSerializerOptions _jsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
  };

  public static ICommand? ToCommand(this Message message)
  {
    var type = ResolveType(message.MessageType, typeof(ICommand));
    if (type is null || string.IsNullOrWhiteSpace(message.Body)) return null;

    // FIX: Passing message.Body (string) instead of a boolean check
    var command = (ICommand?)JsonSerializer.Deserialize(message.Body, type, _jsonOptions);

    if (command != null)
    {
      // Seed the ambient context with the native Guid v7
      CorrelationId.Current = message.CorrelationId;
      // Map the Guid to the command property if it exists
      TrySetCorrelationProperty(command, message.CorrelationId);
    }

    return command;
  }

  public static IEvent? ToEvent(this Message message)
  {
    var type = ResolveType(message.MessageType, typeof(IEvent));
    if (type is null || string.IsNullOrWhiteSpace(message.Body)) return null;

    // FIX: Passing message.Body (string) instead of a boolean check
    var @event = (IEvent?)JsonSerializer.Deserialize(message.Body, type, _jsonOptions);

    if (@event != null)
    {
      CorrelationId.Current = message.CorrelationId;
      TrySetCorrelationProperty(@event, message.CorrelationId);
    }

    return @event;
  }

  private static Type? ResolveType(string? typeName, Type expectedBase)
  {
    if (string.IsNullOrWhiteSpace(typeName)) return null;

    var type = Type.GetType(typeName, throwOnError: false);
    if (type != null && expectedBase.IsAssignableFrom(type)) return type;

    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
      type = asm.GetType(typeName, throwOnError: false);
      if (type != null && expectedBase.IsAssignableFrom(type)) return type;
    }

    return null;
  }

  private static void TrySetCorrelationProperty(object target, Guid correlationId)
  {
    // No more string checks or Guid.Empty checks needed here 
    // because the Message.CorrelationId property already guarantees a valid v7.
    var prop = target.GetType().GetProperty("CorrelationId", BindingFlags.Public | BindingFlags.Instance);

    // HARDENING: Check for Guid type, not string
    if (prop?.CanWrite == true && prop.PropertyType == typeof(Guid))
    {
      prop.SetValue(target, correlationId);
    }
  }
}