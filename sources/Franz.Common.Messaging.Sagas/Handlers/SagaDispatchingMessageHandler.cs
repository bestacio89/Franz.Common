#nullable enable

using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Sagas.Handlers;

public sealed class SagaDispatchingMessageHandler(
    SagaOrchestrator orchestrator,
    IMessageSerializer serializer) : IMessageHandler
{
  private readonly SagaOrchestrator _orchestrator = orchestrator;
  private readonly IMessageSerializer _serializer = serializer;

  public async Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);

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

    var tenantId = message.Headers.TryGetValue(HeaderConstants.TenantId, out var tenantValues) &&
                   tenantValues.Length > 0 &&
                   !string.IsNullOrWhiteSpace(tenantValues[0])
        ? tenantValues[0]
        : null;

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
  }
}
