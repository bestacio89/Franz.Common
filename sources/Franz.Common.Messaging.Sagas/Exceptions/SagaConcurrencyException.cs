namespace Franz.Common.Messaging.Sagas.Exceptions;

/// <summary>
/// Raised when a saga record is created or updated using a stale concurrency version.
/// </summary>
public sealed class SagaConcurrencyException : Exception
{
  public SagaConcurrencyException(Guid sagaId, long? expectedVersion)
      : base(expectedVersion is null
          ? $"Saga '{sagaId}' already exists."
          : $"Saga '{sagaId}' was modified by another execution. Expected version '{expectedVersion}'.")
  {
    SagaId = sagaId;
    ExpectedVersion = expectedVersion;
  }

  public Guid SagaId { get; }
  public long? ExpectedVersion { get; }
}
