using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
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
    if (type is null) return null;

    var command = (ICommand?)JsonSerializer.Deserialize(message.Body is not null, type, _jsonOptions);

    if (command != null)
    {
      CorrelationId.Current = message.CorrelationId;
      TrySetCorrelationProperty(command, message.CorrelationId);
    }

    return command;
  }

  public static IEvent? ToEvent(this Message message)
  {
    var type = ResolveType(message.MessageType, typeof(IEvent));
    if (type is null) return null;

    var @event = (IEvent?)JsonSerializer.Deserialize(message.Body is not null, type, _jsonOptions);

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

    // Try fully qualified name first
    var type = Type.GetType(typeName, throwOnError: false);
    if (type != null && expectedBase.IsAssignableFrom(type)) return type;

    // Fallback: scan all loaded assemblies
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
      type = asm.GetType(typeName, throwOnError: false);
      if (type != null && expectedBase.IsAssignableFrom(type)) return type;
    }

    return null;
  }

  private static void TrySetCorrelationProperty(object target, string? correlationId)
  {
    if (string.IsNullOrEmpty(correlationId)) return;

    var prop = target.GetType().GetProperty("CorrelationId", BindingFlags.Public | BindingFlags.Instance);
    if (prop?.CanWrite == true && prop.PropertyType == typeof(string))
    {
      prop.SetValue(target, correlationId);
    }
  }
}
