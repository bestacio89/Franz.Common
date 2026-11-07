namespace Franz.Common.Messaging.Sagas.Exceptions;

/// <summary>
/// Raised when a message attempts to join a saga using a different workflow correlation.
/// </summary>
public sealed class SagaCorrelationException : InvalidOperationException
{
  public SagaCorrelationException(Guid sagaId, Guid expectedCorrelationId, Guid actualCorrelationId)
      : base($"Saga '{sagaId}' belongs to correlation '{expectedCorrelationId}', not '{actualCorrelationId}'.")
  {
    SagaId = sagaId;
    ExpectedCorrelationId = expectedCorrelationId;
    ActualCorrelationId = actualCorrelationId;
  }

  public Guid SagaId { get; }
  public Guid ExpectedCorrelationId { get; }
  public Guid ActualCorrelationId { get; }
}
