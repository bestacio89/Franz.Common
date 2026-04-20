#nullable enable
using System;
using System.Threading;
using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Core;

public sealed class SagaContext : ISagaContext
{
  public SagaContext(
      Guid sagaId,
      Type sagaType,
      ISagaState state,
      object message,
      Guid correlationId,
      Guid? causationId,
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

  public Guid SagaId { get; }
  public Type SagaType { get; }
  public ISagaState State { get; }
  public object Message { get; }
  public Guid CorrelationId { get; }
  public Guid? CausationId { get; }
  public CancellationToken CancellationToken { get; }

  // Evolutionary Step: Transition metadata enrichment
  public ISagaTransition Continue(object? outgoingMessage = null)
      => CreateTransition(SagaTransition.Continue(outgoingMessage));

  public ISagaTransition Complete(object? outgoingMessage = null)
      => CreateTransition(SagaTransition.Complete(outgoingMessage));

  public ISagaTransition Compensate(object? compensationMessage, Exception? error = null)
      => CreateTransition(SagaTransition.Compensate(compensationMessage, error));

  public ISagaTransition Retry(TimeSpan? delay = null, Exception? error = null)
      => SagaTransition.Retry(delay, error);

  public ISagaTransition Fail(Exception error)
      => SagaTransition.Fail(error);

  /// <summary>
  /// Internal helper to ensure outgoing messages are linked to this Saga context
  /// </summary>
  private ISagaTransition CreateTransition(ISagaTransition transition)
  {
    // Here, the orchestrator will later use these IDs to stamp 
    // the new Message.CorrelationId = this.CorrelationId
    // and Message.CausationId = (This Message's ID)
    return transition;
  }
}