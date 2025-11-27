#nullable enable

using System;

namespace Franz.Common.Messaging.Sagas.Logging;

/// <summary>
/// Represents a single audit entry for a saga step.
/// Used for diagnostics, replay, observability, and traceability.
/// </summary>
public sealed class SagaAuditRecord
{
  public required string SagaId { get; init; }
  public required string SagaType { get; init; }
  public required string StateType { get; init; }
  public required string StepType { get; init; } // Start, Step, Compensate

  public required string IncomingMessageType { get; init; }
  public string? OutgoingMessageType { get; init; }

  public string? CorrelationId { get; init; }
  public string? CausationId { get; init; }

  public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
  public TimeSpan Duration { get; init; }

  /// <summary>
  /// Serialized state after the step is executed.
  /// Useful for debugging and time-travel replay.
  /// </summary>
  public string? SerializedState { get; init; }
}
