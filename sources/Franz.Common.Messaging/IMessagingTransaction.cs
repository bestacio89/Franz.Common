#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging;

/// <summary>
/// Defines a provider-agnostic messaging transaction.
/// Senior Note: Inherits IAsyncDisposable to ensure "Safety Rollback" on scope exit.
/// </summary>
public interface IMessagingTransaction : IAsyncDisposable
{
  /// <summary>
  /// Starts the transaction on the underlying transport.
  /// </summary>
  Task BeginAsync(CancellationToken ct = default);

  /// <summary>
  /// Commits all staged operations to the broker.
  /// </summary>
  Task CompleteAsync(CancellationToken ct = default);

  /// <summary>
  /// Reverts all staged operations.
  /// </summary>
  Task RollbackAsync(CancellationToken ct = default);
}