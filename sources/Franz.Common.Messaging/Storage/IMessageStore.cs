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
  /// Retrieves messages that haven't been sent yet and are not in the DLQ.
  /// Because we use Guid v7, these should naturally return in chronological order.
  /// </summary>
  Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default);

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