#nullable enable

namespace Franz.Common.Messaging.Sagas.Logging;

/// <summary>
/// A durable audit store that participates in the saga persistence boundary and supports
/// bounded retention cleanup. Implementations must enlist writes in the active provider
/// transaction used for saga state, inbox, and outbox persistence.
/// </summary>
public interface ISagaAuditStore : ISagaAuditSink
{
  /// <summary>
  /// Deletes at most <paramref name="batchSize"/> audit records older than the supplied UTC cutoff.
  /// </summary>
  Task<int> DeleteExpiredAsync(
      DateTimeOffset cutoff,
      int batchSize,
      CancellationToken cancellationToken = default);
}
