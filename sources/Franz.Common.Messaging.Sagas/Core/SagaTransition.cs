using Franz.Common.Messaging.Sagas.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Sagas.Core;

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
