namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Durable lifecycle state of a saga instance.
/// </summary>
public enum SagaLifecycleStatus
{
  Running = 0,
  Compensating = 1,
  RetryScheduled = 2,
  Completed = 3,
  Failed = 4
}
