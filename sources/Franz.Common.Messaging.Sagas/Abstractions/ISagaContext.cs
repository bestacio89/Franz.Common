#nullable enable

using System;
using System.Threading;

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Execution context provided to saga step handlers.
/// Carries metadata about the current saga and the triggering message.
/// </summary>
public interface ISagaContext
{
  /// <summary>
  /// The unique identifier of the saga instance.
  /// </summary>
  string SagaId { get; }

  /// <summary>
  /// The saga type being executed.
  /// </summary>
  Type SagaType { get; }

  /// <summary>
  /// The state associated with the saga instance.
  /// </summary>
  ISagaState State { get; }

  /// <summary>
  /// The message that triggered the current step.
  /// </summary>
  object Message { get; }

  /// <summary>
  /// Optional correlation identifier spanning the whole business process.
  /// </summary>
  string? CorrelationId { get; }

  /// <summary>
  /// Optional parent message identifier for tracing.
  /// </summary>
  string? CausationId { get; }

  /// <summary>
  /// Cancellation token for the current execution.
  /// </summary>
  CancellationToken CancellationToken { get; }

  /// <summary>
  /// Creates a transition describing a successful continuation
  /// with an optional outgoing message.
  /// </summary>
  ISagaTransition Continue(object? outgoingMessage = null);

  /// <summary>
  /// Creates a transition that marks the saga as completed,
  /// optionally emitting a final message.
  /// </summary>
  ISagaTransition Complete(object? outgoingMessage = null);

  /// <summary>
  /// Creates a transition that asks the orchestrator to retry
  /// the current step after an optional delay.
  /// </summary>
  ISagaTransition Retry(TimeSpan? delay = null, Exception? error = null);

  /// <summary>
  /// Creates a transition requesting compensation, typically by
  /// emitting a compensating command.
  /// </summary>
  ISagaTransition Compensate(object? compensationMessage, Exception? error = null);

  /// <summary>
  /// Creates a transition that marks the saga as permanently failed.
  /// </summary>
  ISagaTransition Fail(Exception error);
}
