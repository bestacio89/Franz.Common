#nullable enable

using Franz.Common.Headers;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Messages;
using Franz.Common.Messaging.Serialization;
using System.Text.Json;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Validates an internal retry envelope and restores the original event contract and message metadata.
/// </summary>
internal static class SagaRetryMessageRehydrator
{
  public static (IIntegrationEvent Event, Message OriginalMessage) Rehydrate(
      SagaRetryRequested request,
      Message retryMessage,
      IMessageSerializer serializer,
      SagaOrchestrator orchestrator,
      string? tenantId)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(retryMessage);
    ArgumentNullException.ThrowIfNull(serializer);
    ArgumentNullException.ThrowIfNull(orchestrator);

    if (request.SagaId == Guid.Empty ||
        request.OriginalMessageId == Guid.Empty ||
        retryMessage.Id == Guid.Empty)
      throw new PermanentMessagingException("Saga retry envelope contains an empty message or saga identity.");

    if (SagaRetryHeaders.ReadTargetSagaId(retryMessage) != request.SagaId)
      throw new PermanentMessagingException("Saga retry envelope target does not match its transport header.");

    if (string.IsNullOrWhiteSpace(request.OriginalMessageType) ||
        string.IsNullOrWhiteSpace(request.Body) ||
        request.Headers is null ||
        request.Properties is null)
      throw new PermanentMessagingException("Saga retry envelope is missing its original event data.");

    if (!retryMessage.Headers.TryGetValue(Message.CausationHeader, out var retryCausationValues) ||
        retryCausationValues is not { Length: 1 } ||
        !Guid.TryParse(retryCausationValues[0], out var retryCausationId) ||
        retryCausationId != request.OriginalMessageId)
      throw new PermanentMessagingException("Saga retry envelope causation does not match its original message identity.");

    if (!retryMessage.Headers.TryGetValue(Message.CorrelationHeader, out var retryCorrelationValues) ||
        retryCorrelationValues is not { Length: 1 } ||
        !Guid.TryParse(retryCorrelationValues[0], out var retryCorrelationId) ||
        retryMessage.CorrelationId != retryCorrelationId ||
        !request.Headers.TryGetValue(Message.CorrelationHeader, out var originalCorrelationValues) ||
        originalCorrelationValues is not { Length: 1 } ||
        !Guid.TryParse(originalCorrelationValues[0], out var originalCorrelationId) ||
        originalCorrelationId != retryCorrelationId)
      throw new PermanentMessagingException("Saga retry envelope correlation metadata is inconsistent.");

    if (string.IsNullOrWhiteSpace(tenantId))
    {
      if (request.Headers.ContainsKey(HeaderConstants.TenantId))
        throw new PermanentMessagingException("Saga retry envelope contains an unexpected tenant identity.");
    }
    else if (!request.Headers.TryGetValue(HeaderConstants.TenantId, out var originalTenantValues) ||
             originalTenantValues is not { Length: 1 } ||
             !string.Equals(originalTenantValues[0], tenantId, StringComparison.Ordinal))
    {
      throw new PermanentMessagingException("Saga retry envelope tenant does not match the validated tenant context.");
    }

    if (!orchestrator.TryResolveMessageType(request.OriginalMessageType, out var originalType) ||
        originalType is null ||
        !typeof(IIntegrationEvent).IsAssignableFrom(originalType))
      throw new PermanentMessagingException(
          $"Saga retry original message type '{request.OriginalMessageType}' is not a registered saga integration event.");

    IIntegrationEvent originalEvent;
    try
    {
      originalEvent = (IIntegrationEvent?)serializer.Deserialize(request.Body, originalType)
          ?? throw new PermanentMessagingException("Saga retry original event deserialized to null.");
    }
    catch (PermanentMessagingException)
    {
      throw;
    }
    catch (Exception exception)
    {
      throw new PermanentMessagingException(
          $"Saga retry original event could not be deserialized as '{request.OriginalMessageType}'.",
          exception);
    }

    var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    foreach (var property in request.Properties)
    {
      if (string.IsNullOrWhiteSpace(property.Value))
        throw new PermanentMessagingException($"Saga retry property '{property.Key}' is empty.");

      try
      {
        using var document = JsonDocument.Parse(property.Value);
        properties[property.Key] = document.RootElement.Clone();
      }
      catch (JsonException exception)
      {
        throw new PermanentMessagingException(
            $"Saga retry property '{property.Key}' contains invalid JSON.",
            exception);
      }
    }

    properties.Remove(MessagingConstants.TransportGeneratedMessageId);

    var originalMessage = new Message(request.Body, request.Headers)
    {
      Id = retryMessage.Id,
      OccurredOn = request.OccurredOn,
      Kind = request.Kind,
      AggregateId = request.AggregateId,
      Properties = properties
    };

    originalMessage.CorrelationId = retryMessage.CorrelationId;
    originalMessage.MessageType = request.OriginalMessageType;

    if (string.IsNullOrWhiteSpace(tenantId))
      originalMessage.Headers.Remove(HeaderConstants.TenantId);
    else
      originalMessage.Headers[HeaderConstants.TenantId] = [tenantId];

    return (originalEvent, originalMessage);
  }
}