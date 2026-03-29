#nullable enable
using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka.Transactions;

/// <summary>
/// Kafka Producer-level transaction wrapper.
/// Senior Note: Confluent.Kafka is synchronous for transaction control; 
/// we wrap in Task.Run to prevent blocking the .NET 10 thread-pool.
/// </summary>
public sealed class MessagingTransaction : IMessagingTransaction
{
  private readonly IProducer<string, byte[]> _producer;
  private bool _isActive;

  public MessagingTransaction(IProducer<string, byte[]> producer)
  {
    _producer = producer ?? throw new ArgumentNullException(nameof(producer));
  }

  public Task BeginAsync(CancellationToken ct = default)
  {
    // Kafka's BeginTransaction is local-only (no network call), 
    // but we wrap for interface consistency.
    _producer.BeginTransaction();
    _isActive = true;
    return Task.CompletedTask;
  }

  public Task CompleteAsync(CancellationToken ct = default)
  {
    if (!_isActive) return Task.CompletedTask;

    return Task.Run(() =>
    {
      // Timeout is handled via the producer's internal transaction.timeout.ms
      _producer.CommitTransaction();
      _isActive = false;
    }, ct);
  }

  public Task RollbackAsync(CancellationToken ct = default)
  {
    if (!_isActive) return Task.CompletedTask;

    return Task.Run(() =>
    {
      _producer.AbortTransaction();
      _isActive = false;
    }, ct);
  }

  public async ValueTask DisposeAsync()
  {
    if (_isActive)
    {
      // Senior Architect Rule: Always attempt to abort if disposed while active.
      await RollbackAsync();
    }
  }
}