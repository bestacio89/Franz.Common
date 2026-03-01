#nullable enable
using Franz.Common.Mediator;
using Franz.Common.Mediator.Pipelines.Logging; // Added for ambient context
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Sagas.Handlers;

public sealed class SagaDispatchingMessageHandler : IMessageHandler
{
  private readonly SagaOrchestrator _orchestrator;
  private readonly IMessageSerializer _serializer;

  public SagaDispatchingMessageHandler(
      SagaOrchestrator orchestrator,
      IMessageSerializer serializer)
  {
    _orchestrator = orchestrator;
    _serializer = serializer;
  }

  public void Process(Message message)
  {
    // 1. Resolve Type Name (Prioritize the semantic MessageType property)
    var typeName = message.MessageType ?? message.Headers.GetValueOrDefault("type").ToString();
    if (string.IsNullOrWhiteSpace(typeName)) return;

    var type = ResolveType(typeName);
    if (type is null || !typeof(IIntegrationEvent).IsAssignableFrom(type)) return;

    // 2. Deserialize with Null-Safety
    if (string.IsNullOrWhiteSpace(message.Body)) return;
    var evt = (IIntegrationEvent?)_serializer.Deserialize(message.Body!, type);
    if (evt is null) return;

    // 3. Extract IDs - Leveraging the hardened Message properties
    // This property now guarantees a Guid V7 via its lazy-init logic.
    var correlationId = message.CorrelationId;

    // Use the Message ID as the Causation ID (The 'Id' property is already Guid V7)
    var causationId = message.Id;

    // 4. Bridge to Ambient Context
    // This is vital: Seeding the context so the Saga's internal logic is 'connected'
    CorrelationId.Current = correlationId;

    // 5. Hand off to Orchestrator
    // Note: If Orchestrator still uses strings, we .ToString() here, 
    // but the 'Source of Truth' remains the binary Guid.
    try
    {
      _orchestrator
          .HandleEventAsync(
              evt,
              correlationId,
              causationId,
              CancellationToken.None)
          .GetAwaiter()
          .GetResult();
    }
    finally
    {
      // Clear context if you want to prevent leakage, 
      // though AsyncLocal usually handles this per-flow.
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