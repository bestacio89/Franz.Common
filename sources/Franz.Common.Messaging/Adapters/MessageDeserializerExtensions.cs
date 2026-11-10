#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Manifest;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Franz.Common.Messaging.Adapters;

public static class MessageDeserializerExtensions
{
  private static readonly ConcurrentDictionary<string, Lazy<Type?>> TypeResolutionCache = new();

  [RequiresUnreferencedCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  [RequiresDynamicCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  public static ICommand? ToCommand(this Message message)
  {
    ArgumentNullException.ThrowIfNull(message);
    return message.ToCommand(new JsonMessageSerializer()) as ICommand;
  }

  /// <summary>Deserializes a command using the application's configured serializer and generated metadata.</summary>
  public static ICommandMessage? ToCommand(this Message message, IMessageSerializer serializer)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(serializer);

    var type = ResolveType(message.MessageType, typeof(ICommandMessage));
    if (type is null || string.IsNullOrWhiteSpace(message.Body))
      return null;

    var command = serializer.Deserialize(message.Body, type) as ICommandMessage;
    if (command is null)
      return null;

    ApplyCorrelation(command, message.CorrelationId);
    return command;
  }

  [RequiresUnreferencedCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  [RequiresDynamicCode("The parameterless overload uses reflection-based JSON serialization. Use the IMessageSerializer overload with source-generated metadata for Native AOT.")]
  public static IEvent? ToEvent(this Message message)
  {
    ArgumentNullException.ThrowIfNull(message);
    return message.ToEvent(new JsonMessageSerializer());
  }

  /// <summary>Deserializes an event using the application's configured serializer and generated metadata.</summary>
  public static IEvent? ToEvent(this Message message, IMessageSerializer serializer)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(serializer);

    var type = ResolveType(message.MessageType, typeof(IEvent));
    if (type is null || string.IsNullOrWhiteSpace(message.Body))
      return null;

    var @event = serializer.Deserialize(message.Body, type) as IEvent;
    if (@event is null)
      return null;

    ApplyCorrelation(@event, message.CorrelationId);
    return @event;
  }

  private static Type? ResolveType(string? typeName, Type expectedBase)
  {
    if (string.IsNullOrWhiteSpace(typeName))
      return null;

    var cacheKey = $"{typeName}:{expectedBase.FullName}";
    var resolution = TypeResolutionCache.GetOrAdd(cacheKey, _ => new Lazy<Type?>(
        () => ResolveTypeFromManifest(typeName, expectedBase),
        LazyThreadSafetyMode.ExecutionAndPublication));
    var resolvedType = resolution.Value;

    // A manifest may be registered after an earlier lookup. Do not retain negative results.
    if (resolvedType is null)
    {
      ((ICollection<KeyValuePair<string, Lazy<Type?>>>)TypeResolutionCache)
          .Remove(new KeyValuePair<string, Lazy<Type?>>(cacheKey, resolution));
    }

    return resolvedType;
  }

  private static Type? ResolveTypeFromManifest(string typeName, Type expectedBase)
  {
    var matches = MessagingManifestRegistry.GetAll()
        .SelectMany(manifest => expectedBase == typeof(IEvent)
            ? manifest.Subscriptions
            : manifest.KnownTypes)
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

  private static void ApplyCorrelation(object target, Guid? correlationId)
  {
    var targetCorrelation = correlationId ?? Guid.CreateVersion7();
    var currentContext = MediatorContext.Current;
    MediatorContext.Set(currentContext.WithCorrelationId(targetCorrelation));

    if (target is ICorrelatedMessage correlated)
      correlated.CorrelationId = targetCorrelation;
  }
}
