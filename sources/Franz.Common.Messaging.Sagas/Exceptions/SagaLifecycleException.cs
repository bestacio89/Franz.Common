using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Exceptions;

/// <summary>
/// Raised when an incoming message targets a saga that is already terminal.
/// </summary>
public sealed class SagaLifecycleException : InvalidOperationException
{
  public SagaLifecycleException(Guid sagaId, SagaLifecycleStatus status)
      : base($"Saga '{sagaId}' cannot process a message because its lifecycle status is '{status}'.")
  {
    SagaId = sagaId;
    Status = status;
  }

  public Guid SagaId { get; }
  public SagaLifecycleStatus Status { get; }
}
