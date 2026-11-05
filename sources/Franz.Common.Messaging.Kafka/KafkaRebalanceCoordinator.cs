#nullable enable
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Franz.Common.Messaging.Kafka;

/// <summary>
/// Tracks rebalance cancellation per assigned partition, so revoking one partition
/// never aborts work for partitions that remain assigned to this consumer.
/// </summary>
public sealed class KafkaRebalanceCoordinator : IDisposable
{
  private readonly ConcurrentDictionary<string, GroupState> _groups = new(StringComparer.Ordinal);
  private readonly ILogger<KafkaRebalanceCoordinator>? _logger;
  private int _disposed;

  public KafkaRebalanceCoordinator(ILogger<KafkaRebalanceCoordinator>? logger = null)
      => _logger = logger;

  public CancellationToken GetRebalanceToken(string groupId, TopicPartition partition)
  {
    ThrowIfDisposed();
    var state = GetState(groupId);
    lock (state.Sync)
    {
      if (!state.PartitionTokens.TryGetValue(partition, out var source))
      {
        source = new CancellationTokenSource();
        state.PartitionTokens.Add(partition, source);
      }

      return source.Token;
    }
  }

  public void OnPartitionsAssigned(string groupId, IEnumerable<TopicPartition> partitions)
  {
    ThrowIfDisposed();
    ArgumentNullException.ThrowIfNull(partitions);
    var state = GetState(groupId);
    List<CancellationTokenSource>? toDispose = null;

    lock (state.Sync)
    {
      foreach (var partition in partitions)
      {
        if (!state.PartitionTokens.TryGetValue(partition, out var previous))
        {
          state.PartitionTokens.Add(partition, new CancellationTokenSource());
          continue;
        }

        if (!previous.IsCancellationRequested)
          continue;

        state.PartitionTokens[partition] = new CancellationTokenSource();
        if (state.ActiveHandlers == 0)
          (toDispose ??= new List<CancellationTokenSource>()).Add(previous);
        else
          state.RetiredTokens.Add(previous);
      }
    }

    DisposeSources(toDispose);
  }

  public void OnPartitionsRevoked(string groupId, IEnumerable<TopicPartition> partitions)
      => CancelPartitions(groupId, partitions);

  public void OnPartitionsLost(string groupId, IEnumerable<TopicPartition> partitions)
      => CancelPartitions(groupId, partitions);

  public void BeginHandlerExecution(string groupId)
  {
    ThrowIfDisposed();
    var state = GetState(groupId);
    lock (state.Sync)
      state.ActiveHandlers++;
  }

  public void EndHandlerExecution(string groupId)
  {
    if (Volatile.Read(ref _disposed) == 1)
      return;

    var state = GetState(groupId);
    List<CancellationTokenSource>? toDispose = null;
    lock (state.Sync)
    {
      if (state.ActiveHandlers > 0)
        state.ActiveHandlers--;

      if (state.ActiveHandlers == 0 && state.RetiredTokens.Count > 0)
      {
        toDispose = state.RetiredTokens.ToList();
        state.RetiredTokens.Clear();
      }
    }

    DisposeSources(toDispose);
  }

  public bool IsHandlerExecutionActive(string groupId)
  {
    if (Volatile.Read(ref _disposed) == 1)
      return false;

    var state = GetState(groupId);
    lock (state.Sync)
      return state.ActiveHandlers > 0;
  }

  private void CancelPartitions(string groupId, IEnumerable<TopicPartition> partitions)
  {
    ThrowIfDisposed();
    ArgumentNullException.ThrowIfNull(partitions);
    var state = GetState(groupId);
    List<CancellationTokenSource> sources = new();

    lock (state.Sync)
    {
      foreach (var partition in partitions)
        if (state.PartitionTokens.TryGetValue(partition, out var source) &&
            !source.IsCancellationRequested)
          sources.Add(source);
    }

    foreach (var source in sources)
    {
      try
      {
        source.Cancel();
      }
      catch (AggregateException ex)
      {
        // Cancellation callbacks belong to handlers; they must not break Kafka's
        // rebalance callback and prevent the consumer from completing the rebalance.
        _logger?.LogError(ex, "A handler cancellation callback failed during Kafka partition revocation.");
      }
    }
  }

  private GroupState GetState(string groupId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
    return _groups.GetOrAdd(groupId, static _ => new GroupState());
  }

  private static void DisposeSources(IEnumerable<CancellationTokenSource>? sources)
  {
    if (sources is null)
      return;

    foreach (var source in sources)
      source.Dispose();
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
      List<CancellationTokenSource> sources;
      lock (state.Sync)
      {
        sources = state.PartitionTokens.Values.Concat(state.RetiredTokens).Distinct().ToList();
        state.PartitionTokens.Clear();
        state.RetiredTokens.Clear();
        state.ActiveHandlers = 0;
      }

      foreach (var source in sources)
      {
        try { source.Cancel(); }
        catch (AggregateException ex)
        {
          _logger?.LogError(ex, "A handler cancellation callback failed while disposing Kafka rebalance coordination.");
        }
        finally { source.Dispose(); }
      }
    }

    _groups.Clear();
  }

  private sealed class GroupState
  {
    public object Sync { get; } = new();
    public Dictionary<TopicPartition, CancellationTokenSource> PartitionTokens { get; } = new();
    public List<CancellationTokenSource> RetiredTokens { get; } = new();
    public int ActiveHandlers;
  }
}