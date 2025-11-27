#nullable enable

using System;
using System.Threading;
using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Default implementation of <see cref="ISagaContext"/>.
/// Provides the transition factory helpers.
/// </summary>
public sealed class SagaContext : ISagaContext
{
  public SagaContext(
    string sagaId,
    Type sagaType,
    ISagaState state,
    object message,
    string? correlationId,
    string? causationId,
    CancellationToken cancellationToken)
  {
    SagaId = sagaId;
    SagaType = sagaType;
    State = state;
    Message = message;
    CorrelationId = correlationId;
    CausationId = causationId;
    CancellationToken = cancellationToken;
  }

  public string SagaId { get; }
  public Type SagaType { get; }
  public ISagaState State { get; }
  public object Message { get; }
  public string? CorrelationId { get; }
  public string? CausationId { get; }
  public CancellationToken CancellationToken { get; }

  public ISagaTransition Continue(object? outgoingMessage = null)
    => SagaTransition.Continue(outgoingMessage);

  public ISagaTransition Complete(object? outgoingMessage = null)
    => SagaTransition.Complete(outgoingMessage);

  public ISagaTransition Retry(TimeSpan? delay = null, Exception? error = null)
    => SagaTransition.Retry(delay, error);

  public ISagaTransition Compensate(object? compensationMessage, Exception? error = null)
    => SagaTransition.Compensate(compensationMessage, error);

  public ISagaTransition Fail(Exception error)
    => SagaTransition.Fail(error);
}

/// <summary>
/// Concrete immutable saga transition.
/// </summary>
public sealed class SagaTransition : ISagaTransition
{
  private SagaTransition(
    SagaTransitionType type,
    object? outgoingMessage,
    Exception? error,
    TimeSpan? delay)
  {
    Type = type;
    OutgoingMessage = outgoingMessage;
    Error = error;
    Delay = delay;
  }

  public SagaTransitionType Type { get; }
  public object? OutgoingMessage { get; }
  public Exception? Error { get; }
  public TimeSpan? Delay { get; }
  public bool IsTerminal => Type is SagaTransitionType.Complete or SagaTransitionType.Fail;

  public static ISagaTransition Continue(object? message)
    => new SagaTransition(SagaTransitionType.Continue, message, null, null);

  public static ISagaTransition Complete(object? message)
    => new SagaTransition(SagaTransitionType.Complete, message, null, null);

  public static ISagaTransition Retry(TimeSpan? delay, Exception? error)
    => new SagaTransition(SagaTransitionType.Retry, null, error, delay);

  public static ISagaTransition Compensate(object? message, Exception? error)
    => new SagaTransition(SagaTransitionType.Compensate, message, error, null);

  public static ISagaTransition Fail(Exception error)
    => new SagaTransition(SagaTransitionType.Fail, null, error, null);
}
