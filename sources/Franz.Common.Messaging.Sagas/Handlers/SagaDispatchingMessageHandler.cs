#nullable enable
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
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

public sealed class SagaDispatchingMessageHandler(
    SagaOrchestrator orchestrator,
    IMessageSerializer serializer) : IMessageHandler
{
  private readonly SagaOrchestrator _orchestrator = orchestrator;
  private readonly IMessageSerializer _serializer = serializer;

  public async Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    var typeName = message.MessageType;

    if (string.IsNullOrWhiteSpace(typeName) &&
        message.Headers.TryGetValue("type", out var values) &&
        values.Length > 0)
    {
      typeName = values[0];
    }

    if (string.IsNullOrWhiteSpace(typeName))
      return;

    var type = ResolveType(typeName);
    if (type is null || !typeof(IIntegrationEvent).IsAssignableFrom(type))
      return;

    if (string.IsNullOrWhiteSpace(message.Body))
      return;

    var evt = (IIntegrationEvent?)_serializer.Deserialize(message.Body, type);
    if (evt is null)
      return;

    // =========================
    // BOUNDARY NORMALIZATION FIX
    // =========================
    var correlationId = message.CorrelationId ?? Guid.Empty;
    var causationId = message.Id;

    var correlationGuid = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId(); // Ensure a correlation ID is present in the MediatorContext

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
      MediatorContext.Reset(); // Reset the MediatorContext to its previous state after handling the event
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