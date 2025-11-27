#nullable enable

using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Sagas.Logging;

/// <summary>
/// Centralized structured logging definitions for saga orchestration.
/// </summary>
public static partial class SagaLogEvents
{
  private static readonly Action<ILogger, string, string, string?, Exception?> _stepStart =
      LoggerMessage.Define<string, string, string?>(
          LogLevel.Information,
          new EventId(10001, "SagaStepStarted"),
          "Saga {SagaType} [{SagaId}] received message {MessageType}.");

  private static readonly Action<ILogger, string, string, string?, string?, Exception?> _stepComplete =
      LoggerMessage.Define<string, string, string?, string?>(
          LogLevel.Information,
          new EventId(10002, "SagaStepCompleted"),
          "Saga {SagaType} [{SagaId}] step completed. Outgoing: {OutgoingMessageType}. Error: {ErrorMessage}");

  private static readonly Action<ILogger, string, string, string?, Exception?> _compensateStart =
      LoggerMessage.Define<string, string, string?>(
          LogLevel.Warning,
          new EventId(10003, "SagaCompensationStarted"),
          "Saga {SagaType} [{SagaId}] starting compensation for message {MessageType}.");

  private static readonly Action<ILogger, string, string, string?, Exception?> _error =
      LoggerMessage.Define<string, string, string?>(
          LogLevel.Error,
          new EventId(10004, "SagaHandlerError"),
          "Saga {SagaType} [{SagaId}] failed processing message {MessageType}.");

  public static void StepStart(
      ILogger logger,
      string sagaType,
      string sagaId,
      string messageType)
      => _stepStart(logger, sagaType, sagaId, messageType, null);

  public static void StepComplete(
      ILogger logger,
      string sagaType,
      string sagaId,
      string? outgoingMessageType,
      string? errorMessage)
      => _stepComplete(logger, sagaType, sagaId, outgoingMessageType, errorMessage, null);

  public static void CompensationStart(
      ILogger logger,
      string sagaType,
      string sagaId,
      string messageType)
      => _compensateStart(logger, sagaType, sagaId, messageType, null);

  public static void HandlerError(
      ILogger logger,
      string sagaType,
      string sagaId,
      string messageType,
      Exception exception)
      => _error(logger, sagaType, sagaId, messageType, exception);
}
