#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging;

/// <summary>
/// Unified interface for initializing messaging infrastructure (Kafka, RabbitMQ, etc.).
/// Supports async initialization and cancellation tokens.
/// </summary>
public interface IMessagingInitializer
{
  /// <summary>
  /// Initializes the messaging topology (topics, queues, exchanges, subscriptions, etc.).
  /// Should be safe to call multiple times (idempotent).
  /// </summary>
  /// <param name="cancellationToken">Optional cancellation token.</param>
  ValueTask InitializeAsync(CancellationToken cancellationToken = default);
}