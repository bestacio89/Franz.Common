#nullable enable
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Storage;

/// <summary>
/// Defines the storage contract for the Outbox and Dead Letter patterns.
/// Now hardened to use native Guid v7 for high-performance indexing and sorting.
/// </summary>
public interface IMessageStore
{
  /// <summary>
  /// Persists a new message to the outbox. 
  /// The implementation should use the message's native Guid Id.
  /// </summary>
  Task SaveAsync(Message message, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists a message in the outbox without making it eligible for dispatch before
  /// <paramref name="notBefore"/>. Implementations must preserve the schedule in the same
  /// persistence transaction as related domain state.
  /// </summary>
  Task SaveScheduledAsync(
      Message message,
      DateTimeOffset notBefore,
      CancellationToken cancellationToken = default)
      => throw new NotSupportedException("This message store does not support scheduled messages.");

  /// <summary>
  /// Retrieves messages that haven't been sent yet and are not in the DLQ.
  /// Because we use Guid v7, these should naturally return in chronological order.
  /// </summary>
  Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Retrieves at most <paramref name="batchSize"/> pending messages.
  /// Providers should apply the limit in the database query where supported.
  /// </summary>
  async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
    var pending = await GetPendingAsync(cancellationToken).ConfigureAwait(false);
    return pending.Take(batchSize).ToArray();
  }

  /// <summary>Atomically claims pending messages for a dispatcher.</summary>
  Task<IReadOnlyList<StoredMessage>> ClaimPendingAsync(
      Guid leaseOwnerId, TimeSpan leaseDuration, int batchSize, CancellationToken cancellationToken = default)
      => throw new NotSupportedException("This message store does not support atomic outbox claims.");

  /// <summary>Marks a claimed message as sent while its lease is still valid.</summary>
  Task MarkAsSentAsync(Guid messageId, Guid leaseOwnerId, CancellationToken cancellationToken = default)
      => throw new NotSupportedException("This message store does not support ownership-checked completion.");

  /// <summary>Persists retry state and releases a valid lease.</summary>
  Task UpdateRetryAsync(StoredMessage message, Guid leaseOwnerId, CancellationToken cancellationToken = default)
      => throw new NotSupportedException("This message store does not support ownership-checked retry updates.");

  /// <summary>Moves a claimed message to dead-letter storage while its lease is valid.</summary>
  Task MoveToDeadLetterAsync(StoredMessage message, Guid leaseOwnerId, CancellationToken cancellationToken = default)
      => throw new NotSupportedException("This message store does not support ownership-checked dead-lettering.");

  /// <summary>
  /// Marks a message as successfully dispatched using its unique Guid identifier.
  /// </summary>
  // 🛠️ FIX: Changed from string messageId to Guid messageId
  Task MarkAsSentAsync(Guid messageId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persist retry-related fields for a message (RetryCount, LastError, LastTriedOn).
  /// Uses the native Guid Id within the StoredMessage object.
  /// </summary>
  Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default);

  /// <summary>
  /// Flags a failed message as Dead Letter. 
  /// This prevents the Outbox from polling it again while preserving it for manual triage.
  /// </summary>
  Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default);
}