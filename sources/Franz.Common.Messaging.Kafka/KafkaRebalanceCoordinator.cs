#nullable enable
using System.Collections.Concurrent;

namespace Franz.Common.Messaging.Kafka;

/// <summary>Coordinates rebalance cancellation with the active handler for each consumer group.</summary>
public sealed class KafkaRebalanceCoordinator : IDisposable
{
  private readonly ConcurrentDictionary<string, GroupState> _groups = new(StringComparer.Ordinal);
  private int _disposed;

  public CancellationToken GetRebalanceToken(string groupId)
  {
    ThrowIfDisposed();
    var state = GetState(groupId);
    lock (state.Sync)
      return state.Rebalance.Token;
  }

  public void OnPartitionsAssigned(string groupId)
  {
    ThrowIfDisposed();
    var state = GetState(groupId);
    lock (state.Sync)
    {
      var previous = state.Rebalance;
      state.Rebalance = new CancellationTokenSource();
      previous.Cancel();
      previous.Dispose();
    }
  }

  public void OnPartitionsRevoked(string groupId) => CancelGroup(groupId);
  public void OnPartitionsLost(string groupId) => CancelGroup(groupId);

  public void BeginHandlerExecution(string groupId)
  {
    ThrowIfDisposed();
    Interlocked.Exchange(ref GetState(groupId).ActiveHandler, 1);
  }

  public void EndHandlerExecution(string groupId)
      => Interlocked.Exchange(ref GetState(groupId).ActiveHandler, 0);

  public bool IsHandlerExecutionActive(string groupId)
      => Volatile.Read(ref GetState(groupId).ActiveHandler) == 1;

  private void CancelGroup(string groupId)
  {
    ThrowIfDisposed();
    var state = GetState(groupId);
    lock (state.Sync)
      if (!state.Rebalance.IsCancellationRequested)
        state.Rebalance.Cancel();
  }

  private GroupState GetState(string groupId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
    return _groups.GetOrAdd(groupId, static _ => new GroupState());
  }

  private void ThrowIfDisposed()
  {
    if (Volatile.Read(ref _disposed) == 1)
      throw new ObjectDisposedException(nameof(KafkaRebalanceCoordinator));
  }

  public void Dispose()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1)
      return;
    foreach (var state in _groups.Values)
    {
      lock (state.Sync)
      {
        state.Rebalance.Cancel();
        state.Rebalance.Dispose();
      }
    }
    _groups.Clear();
  }

  private sealed class GroupState
  {
    public object Sync { get; } = new();
    public CancellationTokenSource Rebalance { get; set; } = new();
    public int ActiveHandler;
  }
}