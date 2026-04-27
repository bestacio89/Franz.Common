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
    if (type is null || string.IsNullOrWhiteSpace(message.Body))
      return null;

    var command = (ICommand?)JsonSerializer.Deserialize(message.Body, type, _jsonOptions);

    if (command is null)
      return null;

    ApplyCorrelation(command, message.CorrelationId);

    return command;
  }

  public static IEvent? ToEvent(this Message message)
  {
    var type = ResolveType(message.MessageType, typeof(IEvent));
    if (type is null || string.IsNullOrWhiteSpace(message.Body))
      return null;

    var @event = (IEvent?)JsonSerializer.Deserialize(message.Body, type, _jsonOptions);

    if (@event is null)
      return null;

    ApplyCorrelation(@event, message.CorrelationId);

    return @event;
  }

  // =====================================================
  // TYPE RESOLUTION (transport-safe)
  // =====================================================

  private static Type? ResolveType(string? typeName, Type expectedBase)
  {
    if (string.IsNullOrWhiteSpace(typeName))
      return null;

    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
      var type = asm.GetType(typeName, throwOnError: false);

      if (type != null && expectedBase.IsAssignableFrom(type))
        return type;
    }

    // fallback: search by FullName (more test-friendly)
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
      var type = asm.GetTypes()
        .FirstOrDefault(t =>
          t.FullName == typeName &&
          expectedBase.IsAssignableFrom(t));

      if (type != null)
        return type;
    }

    return null;
  }

  // =====================================================
  // CORRELATION (transport-agnostic injection)
  // =====================================================

  private static void ApplyCorrelation(object target, Guid? correlationId)
  {
    // -----------------------------
    // 1. Ambient context (ONLY if present)
    // -----------------------------
    if (correlationId is Guid correlation)
    {
      CorrelationId.Current = correlation;
    }

    var prop = target.GetType()
      .GetProperty("CorrelationId", BindingFlags.Public | BindingFlags.Instance);

    if (prop is null || !prop.CanWrite)
      return;

    // -----------------------------
    // 2. Domain hydration
    // -----------------------------
    if (prop.PropertyType == typeof(Guid))
    {
      prop.SetValue(target, correlationId ?? Guid.Empty);
    }
    else if (prop.PropertyType == typeof(Guid?))
    {
      prop.SetValue(target, correlationId);
    }
  }
}