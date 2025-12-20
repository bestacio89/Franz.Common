using Franz.Common.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace Franz.Common.Messaging.Storage;

public interface IMessageStore
{
  Task SaveAsync(Message message, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default);

  Task MarkAsSentAsync(string messageId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persist retry-related fields for a message (RetryCount, LastError, LastTriedOn).
  /// The implementation should use message.Id as the identifier.
  /// </summary>
  Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default);

  /// <summary>
  /// Move a failed message to a dead-letter collection/queue and remove it from the outbox.
  /// </summary>
  Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default);
}
