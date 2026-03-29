#nullable enable
using Franz.Common.Mediator;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Serialization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Handlers;

/// <summary>
/// Dispatches incoming messages to the Saga Orchestrator.
/// Senior Architect Note: Refactored to native Async v7+ to prevent thread-pool starvation.
/// Leverages Guid-based CorrelationId context with guaranteed cleanup.
/// </summary>
public sealed class SagaDispatchingMessageHandler(
    SagaOrchestrator orchestrator,
    IMessageSerializer serializer) : IMessageHandler
{
  private readonly SagaOrchestrator _orchestrator = orchestrator;
  private readonly IMessageSerializer _serializer = serializer;

  public async Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    // 1. Resolve Type Name 
    var typeName = message.MessageType;

    // Fallback to manual header lookup
    if (string.IsNullOrWhiteSpace(typeName) &&
        message.Headers.TryGetValue("type", out var values) && values.Length > 0)
    {
      typeName = values[0];
    }

    if (string.IsNullOrWhiteSpace(typeName)) return;

    var type = ResolveType(typeName);
    if (type is null || !typeof(IIntegrationEvent).IsAssignableFrom(type)) return;

    // 2. Deserialize with Null-Safety
    if (string.IsNullOrWhiteSpace(message.Body)) return;

    var evt = (IIntegrationEvent?)_serializer.Deserialize(message.Body, type);
    if (evt is null) return;

    // 3. Extract IDs 
    var correlationId = message.CorrelationId;
    var causationId = message.Id;

    // 4. Bridge to Ambient Context & Dispatch
    // Senior Note: We wrap the orchestrator call to ensure the AsyncLocal CorrelationId 
    // is cleared even if the orchestrator throws.
    CorrelationId.Current = correlationId;
    try
    {
      await _orchestrator.HandleEventAsync(
          evt,
          correlationId,
          causationId,
          ct).ConfigureAwait(false);
    }
    finally
    {
      // Clear context to prevent leakage across threads in high-concurrency environments
      CorrelationId.Current = Guid.Empty;
    }
  }

  private static Type? ResolveType(string typeName)
  {
    var type = Type.GetType(typeName, throwOnError: false);
    if (type != null) return type;

    return AppDomain.CurrentDomain.GetAssemblies()
        .Select(asm => asm.GetType(typeName, throwOnError: false))
        .FirstOrDefault(t => t != null);
  }
}