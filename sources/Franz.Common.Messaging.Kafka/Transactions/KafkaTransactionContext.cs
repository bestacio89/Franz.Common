#nullable enable
using Confluent.Kafka;

namespace Franz.Common.Messaging.Kafka.Transactions;

/// <summary>
/// Scoped access to the producer participating in the current Kafka transaction.
/// </summary>
public sealed class KafkaTransactionContext
{
  private IProducer<string, byte[]>? _producer;

  internal IProducer<string, byte[]>? Producer => Volatile.Read(ref _producer);

  internal void SetProducer(IProducer<string, byte[]> producer)
  {
    ArgumentNullException.ThrowIfNull(producer);
    if (Interlocked.CompareExchange(ref _producer, producer, null) is not null)
      throw new InvalidOperationException("A Kafka transaction is already active in this scope.");
  }

  internal void ClearProducer(IProducer<string, byte[]> producer)
  {
    Interlocked.CompareExchange(ref _producer, null, producer);
  }
}
