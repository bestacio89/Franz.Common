#nullable enable

using System;

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Represents the result of executing a saga step.
/// Immutable description of what the orchestrator should do next.
/// </summary>
public interface ISagaTransition
{
  /// <summary>
  /// The transition type describing the outcome of the step.
  /// </summary>
  SagaTransitionType Type { get; }

  /// <summary>
  /// Optional outgoing message (command or event) that should be
  /// dispatched by the messaging layer.
  /// </summary>
  object? OutgoingMessage { get; }

  /// <summary>
  /// Optional delay used when <see cref="SagaTransitionType.Retry"/> is selected.
  /// </summary>
  TimeSpan? Delay { get; }

  /// <summary>
  /// Optional error associated with a failed transition.
  /// </summary>
  Exception? Error { get; }

  /// <summary>
  /// Indicates whether this transition ends the saga lifecycle
  /// (for example Complete or Fail).
  /// </summary>
  bool IsTerminal { get; }
}
