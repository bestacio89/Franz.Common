namespace Franz.Common.Messaging.Sagas.Exceptions;

/// <summary>
/// Raised after a failed saga transition has been durably recorded.
/// </summary>
public sealed class SagaFailedException : Exception
{
  public SagaFailedException(Guid sagaId, Exception? innerException)
      : base($"Saga '{sagaId}' entered the Failed lifecycle state.", innerException)
  {
    SagaId = sagaId;
  }

  public Guid SagaId { get; }
}
