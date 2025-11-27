#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Persistence.RabbitMq;

/// <summary>
/// RabbitMQ cannot serve as a saga-state storage engine because it provides
/// no durable, queryable, or updatable key-value storage.
/// This class exists only to prevent accidental usage.
/// </summary>
internal sealed class RabbitMqSagaRepository : ISagaRepository
{
  public Task<object?> LoadStateAsync(
      string sagaId,
      Type stateType,
      CancellationToken cancellationToken)
  {
    throw new NotSupportedException(
        "RabbitMQ cannot be used as a saga state repository. " +
        "Use EF, Redis, Memory, or Kafka compacted topics instead.");
  }

  public Task SaveStateAsync(
      string sagaId,
      object state,
      CancellationToken cancellationToken)
  {
    throw new NotSupportedException(
        "RabbitMQ cannot be used as a saga state repository. " +
        "Use EF, Redis, Memory, or Kafka compacted topics instead.");
  }

  public Task DeleteStateAsync(
      string sagaId,
      CancellationToken cancellationToken)
  {
    // Delete is meaningless; but we throw to be explicit.
    throw new NotSupportedException(
        "RabbitMQ cannot be used as a saga state repository.");
  }
}
