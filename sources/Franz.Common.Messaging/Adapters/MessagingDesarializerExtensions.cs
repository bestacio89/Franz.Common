#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Messaging.Adapters;

public static class MessageDeserializerExtensions
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  // Cache reflected property setters to avoid runtime reflection overhead per message deserialization
  private static readonly ConcurrentDictionary<Type, Action<object, Guid>?> PropertySetterCache = new();
  private static readonly ConcurrentDictionary<string, Type?> TypeResolutionCache = new();

  public static ICommand? ToCommand(this Message message)
  {
    var type = ResolveType(message.MessageType, typeof(ICommand));
    if (type is null || string.IsNullOrWhiteSpace(message.Body))
      return null;

    var command = (ICommand?)JsonSerializer.Deserialize(message.Body, type, JsonOptions);
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

    var @event = (IEvent?)JsonSerializer.Deserialize(message.Body, type, JsonOptions);
    if (@event is null)
      return null;

    ApplyCorrelation(@event, message.CorrelationId);
    return @event;
  }

  // =====================================================
  // TYPE RESOLUTION (Cached & Transport-Safe)
  // =====================================================

  private static Type? ResolveType(string? typeName, Type expectedBase)
  {
    if (string.IsNullOrWhiteSpace(typeName))
      return null;

    var cacheKey = $"{typeName}:{expectedBase.FullName}";
    return TypeResolutionCache.GetOrAdd(cacheKey, _ =>
    {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();

      for (int i = 0; i < assemblies.Length; i++)
      {
        var type = assemblies[i].GetType(typeName, throwOnError: false);
        if (type != null && expectedBase.IsAssignableFrom(type))
          return type;
      }

      // Fallback: search by FullName
      for (int i = 0; i < assemblies.Length; i++)
      {
        var types = assemblies[i].GetTypes();
        for (int j = 0; j < types.Length; j++)
        {
          var t = types[j];
          if (t.FullName == typeName && expectedBase.IsAssignableFrom(t))
            return t;
        }
      }

      return null;
    });
  }

  // =====================================================
  // CORRELATION (MediatorContext Ambient Hydration)
  // =====================================================

  private static void ApplyCorrelation(object target, Guid? correlationId)
  {
    var targetCorrelation = correlationId ?? Guid.CreateVersion7();

    // 1. Hydrate Ambient MediatorContext for downstream pipelines / telemetry
    var currentContext = MediatorContext.Current;
    MediatorContext.Set(currentContext.WithCorrelationId(targetCorrelation));

    // 2. Hydrate Domain Property on the Message Payload (Cached Setter)
    var targetType = target.GetType();
    var setter = PropertySetterCache.GetOrAdd(targetType, static type => CompileCorrelationSetter(type));

    setter?.Invoke(target, targetCorrelation);
  }

  private static Action<object, Guid>? CompileCorrelationSetter(Type type)
  {
    var prop = type.GetProperty("CorrelationId", BindingFlags.Public | BindingFlags.Instance);
    if (prop is null || !prop.CanWrite)
      return null;

    if (prop.PropertyType == typeof(Guid))
    {
      return (obj, val) => prop.SetValue(obj, val);
    }

    if (prop.PropertyType == typeof(Guid?))
    {
      return (obj, val) => prop.SetValue(obj, (Guid?)val);
    }

    return null;
  }
}