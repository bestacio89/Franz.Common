#nullable enable

namespace Franz.Common.Messaging.Storage;

/// <summary>
/// Stores durable receipts for messages that have been accepted for processing.
/// Implementations must enforce uniqueness of <see cref="TryRegisterAsync"/> atomically.
/// </summary>
public interface IInboxStore
{
  /// <summary>
  /// Checks whether a message has already been registered as processed.
  /// This method is informational and must not be used as the concurrency guard
  /// for message execution; use <see cref="TryRegisterAsync"/> instead.
  /// </summary>
  Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default);

  /// <summary>
  /// Atomically registers a message receipt.
  /// Returns <see langword="true"/> only for the caller that created the receipt;
  /// returns <see langword="false"/> when the message was already registered.
  /// Concurrent calls for the same identifier must have exactly one successful result.
  /// </summary>
  /// <param name="messageId">The stable, non-empty message identifier.</param>
  /// <param name="ct">Cancellation token.</param>
  Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default);

  /// <summary>
  /// Records a processed message. Existing implementations may retain their
  /// provider-specific duplicate behavior; new processing pipelines should prefer
  /// <see cref="TryRegisterAsync"/> for atomic deduplication.
  /// </summary>
  Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default);
}
