#nullable enable
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.RabbitMQ.Transactions;

/// <summary>
/// RabbitMQ Channel-level transaction wrapper.
/// Senior Note: Native Async v7 implementation. Using ValueTask for hot-path state checks.
/// </summary>
public sealed class RabbitMQMessagingTransaction : IMessagingTransaction, IAsyncDisposable
{
  private IChannel? _activeChannel;
  private bool _isActive;

  /// <summary>
  /// Attaches the transaction to the rented channel from the pool.
  /// </summary>
  public void Attach(IChannel channel)
  {
    _activeChannel = channel ?? throw new ArgumentNullException(nameof(channel));
  }

  public async Task BeginAsync(CancellationToken ct = default)
  {
    if (_activeChannel == null)
      throw new InvalidOperationException("No active channel attached to the transaction. Use Attach(channel) first.");

    // RabbitMQ Select (Enable Transactions on Channel)
    await _activeChannel.TxSelectAsync(ct);
    _isActive = true;
  }

  public async Task CompleteAsync(CancellationToken ct = default)
  {
    if (!_isActive || _activeChannel == null) return;

    try
    {
      await _activeChannel.TxCommitAsync(ct);
    }
    finally
    {
      _isActive = false;
    }
  }

  public async Task RollbackAsync(CancellationToken ct = default)
  {
    if (!_isActive || _activeChannel == null) return;

    try
    {
      await _activeChannel.TxRollbackAsync(ct);
    }
    finally
    {
      _isActive = false;
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_isActive)
    {
      // Senior Architect Rule: Automatic safety rollback on disposal if still active
      await RollbackAsync();
    }

    _activeChannel = null;
  }
}