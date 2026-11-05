#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Manifest;
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
  private static readonly ConcurrentDictionary<string, Lazy<Type?>> TypeResolutionCache = new();

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
    return TypeResolutionCache.GetOrAdd(cacheKey, _ => new Lazy<Type?>(
        () => ResolveTypeFromManifest(typeName, expectedBase),
        LazyThreadSafetyMode.ExecutionAndPublication)).Value;
  }

  private static Type? ResolveTypeFromManifest(string typeName, Type expectedBase)
  {
    var matches = MessagingManifestRegistry.GetAll()
        .SelectMany(static manifest => manifest.KnownTypes)
        .Where(type =>
            expectedBase.IsAssignableFrom(type) &&
            (string.Equals(type.FullName, typeName, StringComparison.Ordinal) ||
             string.Equals(type.AssemblyQualifiedName, typeName, StringComparison.Ordinal)))
        .Distinct()
        .ToArray();

    if (matches.Length > 1)
      throw new InvalidOperationException(
          $"Message type '{typeName}' is ambiguous in the generated messaging manifests.");

    return matches.Length == 1 ? matches[0] : null;
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