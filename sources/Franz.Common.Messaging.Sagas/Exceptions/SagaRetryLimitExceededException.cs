namespace Franz.Common.Messaging.Sagas.Exceptions;

/// <summary>
/// Raised when a saga step requests another retry after exhausting its configured retry budget.
/// </summary>
public sealed class SagaRetryLimitExceededException : Exception
{
  public SagaRetryLimitExceededException(
      Guid sagaId,
      int maxRetryAttempts,
      Exception? lastError = null)
      : base(
          $"Saga '{sagaId}' exceeded its maximum of {maxRetryAttempts} scheduled retry attempts.",
          lastError)
  {
    SagaId = sagaId;
    MaxRetryAttempts = maxRetryAttempts;
  }

  public Guid SagaId { get; }
  public int MaxRetryAttempts { get; }
}