#nullable enable

using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Storage;

namespace Franz.Common.Messaging.Sagas.Handlers;

public sealed class SagaDispatchingMessageHandler(
    SagaOrchestrator orchestrator,
    IMessageSerializer serializer,
    IInboxStore? inboxStore = null) : IMessageHandler
{
  private readonly SagaOrchestrator _orchestrator = orchestrator;
  private readonly IMessageSerializer _serializer = serializer;
  private readonly IInboxStore? _inboxStore = inboxStore;

  public async Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (_inboxStore is not null &&
        await _inboxStore.HasProcessedAsync(message.Id, ct).ConfigureAwait(false))
      return;

    var typeName = message.MessageType;
    if (string.IsNullOrWhiteSpace(typeName) &&
        message.Headers.TryGetValue("type", out var typeValues) &&
        typeValues.Length > 0)
      typeName = typeValues[0];

    if (string.IsNullOrWhiteSpace(typeName))
      throw new PermanentMessagingException("Saga message does not declare a message type.");

    if (!_orchestrator.TryResolveMessageType(typeName, out var type) || type is null)
      throw new PermanentMessagingException(
          $"Saga message type '{typeName}' could not be resolved.");

    if (!typeof(IIntegrationEvent).IsAssignableFrom(type))
      throw new PermanentMessagingException(
          $"Saga message type '{typeName}' does not implement IIntegrationEvent.");

    if (string.IsNullOrWhiteSpace(message.Body))
      throw new PermanentMessagingException(
          $"Saga message '{message.Id}' has an empty body.");

    IIntegrationEvent integrationEvent;
    try
    {
      integrationEvent = (IIntegrationEvent?)_serializer.Deserialize(message.Body, type)
          ?? throw new PermanentMessagingException(
              $"Saga message '{message.Id}' deserialized to null.");
    }
    catch (PermanentMessagingException)
    {
      throw;
    }
    catch (Exception exception)
    {
      throw new PermanentMessagingException(
          $"Saga message '{message.Id}' could not be deserialized as '{typeName}'.",
          exception);
    }

    var correlationId = message.CorrelationId.GetValueOrDefault();
    if (correlationId == Guid.Empty)
      correlationId = Guid.CreateVersion7();

    Guid? causationId = message.Headers.TryGetValue(Message.CausationHeader, out var causationValues) &&
                      causationValues.Length > 0 &&
                      Guid.TryParse(causationValues[0], out var parsedCausationId)
        ? parsedCausationId
        : null;

    string? tenantId = null;
    if (message.Headers.TryGetValue(HeaderConstants.TenantId, out var tenantValues))
    {
      if (tenantValues.Length != 1 || string.IsNullOrWhiteSpace(tenantValues[0]))
        throw new PermanentMessagingException(
            "Saga message tenant metadata must contain exactly one non-empty tenant identifier.");

      tenantId = tenantValues[0];
    }

    var executionContext = MediatorContext.Current
        .WithCorrelationId(correlationId)
        .WithCausationId(causationId)
        .WithTenant(tenantId);

    using var contextScope = MediatorContext.BeginScope(executionContext);

    await _orchestrator.HandleEventAsync(
        integrationEvent,
        correlationId,
        causationId,
        message.Id,
        tenantId,
        ct).ConfigureAwait(false);

    // Register only after successful orchestration so handler failures remain retryable.
    // The receipt is not transactionally coupled to saga/outbox persistence by this contract.
    if (_inboxStore is not null)
      _ = await _inboxStore.TryRegisterAsync(message.Id, ct).ConfigureAwait(false);
  }
}
