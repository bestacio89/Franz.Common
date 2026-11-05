#nullable enable
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Franz.Common.Messaging.Kafka;

/// <summary>
/// Tracks rebalance cancellation and active handler execution per Kafka consumer instance.
/// Consumer group identifiers are diagnostic metadata only; they are not state ownership keys.
/// </summary>
public sealed class KafkaRebalanceCoordinator : IDisposable
{
  private readonly ConditionalWeakTable<IConsumer<string, string>, ConsumerRegistration> _registrations = new();
  private readonly ConcurrentDictionary<Guid, ConsumerRegistration> _activeRegistrations = new();
  private readonly ILogger<KafkaRebalanceCoordinator>? _logger;
  private int _disposed;

  public KafkaRebalanceCoordinator(ILogger<KafkaRebalanceCoordinator>? logger = null)
      => _logger = logger;

  /// <summary>Creates isolated rebalance state for one Kafka consumer.</summary>
  public ConsumerRegistration CreateRegistration(string groupId)
  {
    ThrowIfDisposed();
    ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

    var registration = new ConsumerRegistration(Guid.CreateVersion7(), groupId);
    if (!_activeRegistrations.TryAdd(registration.Id, registration))
      throw new InvalidOperationException("A duplicate Kafka consumer registration identifier was generated.");

    return registration;
  }

  /// <summary>Associates the consumer returned by the factory with its isolated rebalance state.</summary>
  public void RegisterConsumer(
      IConsumer<string, string> consumer,
      ConsumerRegistration registration)
  {
    ThrowIfDisposed();
    ArgumentNullException.ThrowIfNull(consumer);
    ArgumentNullException.ThrowIfNull(registration);

    if (!_activeRegistrations.TryGetValue(registration.Id, out var active) ||
        !ReferenceEquals(active, registration))
      throw new InvalidOperationException("The Kafka consumer registration is not active.");

    _registrations.Add(consumer, registration);
  }

  /// <summary>Gets the existing registration or creates one for externally constructed consumers.</summary>
  public ConsumerRegistration GetOrCreateRegistration(
      IConsumer<string, string> consumer,
      string groupId)
  {
    ThrowIfDisposed();
    ArgumentNullException.ThrowIfNull(consumer);

    if (_registrations.TryGetValue(consumer, out var existing))
      return existing;

    var registration = CreateRegistration(groupId);
    try
    {
      RegisterConsumer(consumer, registration);
      return registration;
    }
    catch
    {
      ReleaseRegistration(registration);
      throw;
    }
  }

  public CancellationToken GetRebalanceToken(
      ConsumerRegistration registration,
      TopicPartition partition)
  {
    var state = GetActiveRegistration(registration);
    lock (state.Sync)
    {
      EnsureActiveRegistration(state);
      if (state.RevokedTokens.TryGetValue(partition, out var revokedToken))
        return revokedToken;

      if (!state.PartitionTokens.TryGetValue(partition, out var source))
      {
        source = new CancellationTokenSource();
        state.PartitionTokens.Add(partition, source);
      }

      return source.Token;
    }
  }

  public void OnPartitionsAssigned(
      ConsumerRegistration registration,
      IEnumerable<TopicPartition> partitions)
  {
    var state = GetActiveRegistration(registration);
    ArgumentNullException.ThrowIfNull(partitions);
    List<CancellationTokenSource>? toDispose = null;

    lock (state.Sync)
    {
      EnsureActiveRegistration(state);
      foreach (var partition in partitions)
      {
        state.RevokedTokens.Remove(partition);

        if (state.PartitionTokens.TryGetValue(partition, out var previous))
        {
          if (!previous.IsCancellationRequested)
            continue;

          RetireToken(state, previous, ref toDispose);
        }

        state.PartitionTokens[partition] = new CancellationTokenSource();
      }
    }

    DisposeSources(toDispose);
  }

  public void OnPartitionsRevoked(
      ConsumerRegistration registration,
      IEnumerable<TopicPartition> partitions)
      => CancelPartitions(registration, partitions);

  public void OnPartitionsLost(
      ConsumerRegistration registration,
      IEnumerable<TopicPartition> partitions)
      => CancelPartitions(registration, partitions);

  public void BeginHandlerExecution(ConsumerRegistration registration)
  {
    var state = GetActiveRegistration(registration);
    lock (state.Sync)
    {
      EnsureActiveRegistration(state);
      state.ActiveHandlers++;
    }
  }

  public void EndHandlerExecution(ConsumerRegistration registration)
  {
    ArgumentNullException.ThrowIfNull(registration);
    if (!_activeRegistrations.TryGetValue(registration.Id, out var state) ||
        !ReferenceEquals(state, registration))
      return;

    List<CancellationTokenSource>? toDispose = null;
    var removeRegistration = false;
    lock (state.Sync)
    {
      if (state.ActiveHandlers > 0)
        state.ActiveHandlers--;

      if (state.ActiveHandlers == 0)
      {
        toDispose = state.RetiredTokens.ToList();
        state.RetiredTokens.Clear();

        if (state.DisposeWhenIdle)
        {
          (toDispose ??= new List<CancellationTokenSource>())
              .AddRange(state.PartitionTokens.Values);
          state.PartitionTokens.Clear();
          state.RevokedTokens.Clear();
          removeRegistration = true;
        }
      }
    }

    if (removeRegistration)
      _activeRegistrations.TryRemove(registration.Id, out _);

    DisposeSources(toDispose);
  }

  public bool IsHandlerExecutionActive(ConsumerRegistration registration)
  {
    if (Volatile.Read(ref _disposed) == 1 ||
        !_activeRegistrations.TryGetValue(registration.Id, out var state) ||
        !ReferenceEquals(state, registration))
      return false;

    lock (state.Sync)
      return state.ActiveHandlers > 0;
  }

  /// <summary>Releases all state associated with a consumer after its listener has stopped.</summary>
  public void ReleaseConsumer(IConsumer<string, string> consumer)
  {
    ArgumentNullException.ThrowIfNull(consumer);
    if (_registrations.TryGetValue(consumer, out var registration))
    {
      _registrations.Remove(consumer);
      ReleaseRegistration(registration);
    }
  }

  /// <summary>Releases a registration when consumer construction fails.</summary>
  public void ReleaseRegistration(ConsumerRegistration registration)
  {
    ArgumentNullException.ThrowIfNull(registration);
    if (!_activeRegistrations.TryGetValue(registration.Id, out var state) ||
        !ReferenceEquals(state, registration))
      return;

    List<CancellationTokenSource> sources;
    var disposeImmediately = false;
    lock (state.Sync)
    {
      state.DisposeWhenIdle = true;
      sources = state.PartitionTokens.Values
          .Concat(state.RetiredTokens)
          .Distinct()
          .ToList();

      if (state.ActiveHandlers == 0)
      {
        state.PartitionTokens.Clear();
        state.RevokedTokens.Clear();
        state.RetiredTokens.Clear();
        disposeImmediately = true;
      }
    }

    if (disposeImmediately)
      _activeRegistrations.TryRemove(registration.Id, out _);

    if (disposeImmediately)
      CancelAndDisposeSources(sources);
    else
      CancelSources(sources);
  }

  private void CancelPartitions(
      ConsumerRegistration registration,
      IEnumerable<TopicPartition> partitions)
  {
    var state = GetActiveRegistration(registration);
    ArgumentNullException.ThrowIfNull(partitions);
    List<CancellationTokenSource> sources = new();
    List<CancellationTokenSource>? toDispose = null;

    lock (state.Sync)
    {
      EnsureActiveRegistration(state);
      foreach (var partition in partitions)
      {
        if (!state.PartitionTokens.Remove(partition, out var source))
          continue;

        state.RevokedTokens[partition] = source.Token;
        if (!source.IsCancellationRequested)
          sources.Add(source);

        RetireToken(state, source, ref toDispose);
      }
    }

    foreach (var source in sources)
    {
      try
      {
        source.Cancel();
      }
      catch (AggregateException ex)
      {
        _logger?.LogError(ex,
            "A handler cancellation callback failed while revoking Kafka partitions for consumer {ConsumerId}.",
            registration.Id);
      }
    }

    DisposeSources(toDispose);
  }

  private static void RetireToken(
      ConsumerRegistration state,
      CancellationTokenSource source,
      ref List<CancellationTokenSource>? toDispose)
  {
    if (state.ActiveHandlers == 0)
      (toDispose ??= new List<CancellationTokenSource>()).Add(source);
    else
      state.RetiredTokens.Add(source);
  }

  private ConsumerRegistration GetActiveRegistration(ConsumerRegistration registration)
  {
    ThrowIfDisposed();
    ArgumentNullException.ThrowIfNull(registration);

    if (!_activeRegistrations.TryGetValue(registration.Id, out var state) ||
        !ReferenceEquals(state, registration))
      throw new InvalidOperationException("The Kafka consumer registration is no longer active.");

    return state;
  }

  private void EnsureActiveRegistration(ConsumerRegistration registration)
  {
    ThrowIfDisposed();
    if (!_activeRegistrations.TryGetValue(registration.Id, out var state) ||
        !ReferenceEquals(state, registration))
      throw new InvalidOperationException("The Kafka consumer registration is no longer active.");
  }

  private static void DisposeSources(IEnumerable<CancellationTokenSource>? sources)
  {
    if (sources is null)
      return;

    foreach (var source in sources)
      source.Dispose();
  }

  private void CancelAndDisposeSources(IEnumerable<CancellationTokenSource> sources)
  {
    foreach (var source in sources)
    {
      try { source.Cancel(); }
      catch (AggregateException ex)
      {
        _logger?.LogError(ex, "A handler cancellation callback failed while releasing Kafka consumer state.");
      }
      finally { source.Dispose(); }
    }
  }

  private void CancelSources(IEnumerable<CancellationTokenSource> sources)
  {
    foreach (var source in sources)
    {
      try { source.Cancel(); }
      catch (AggregateException ex)
      {
        _logger?.LogError(ex, "A handler cancellation callback failed while releasing active Kafka consumer state.");
      }
    }
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

    foreach (var registration in _activeRegistrations.Values.ToArray())
    {
      List<CancellationTokenSource> sources;
      var disposeImmediately = false;
      lock (registration.Sync)
      {
        registration.DisposeWhenIdle = true;
        sources = registration.PartitionTokens.Values
            .Concat(registration.RetiredTokens)
            .Distinct()
            .ToList();

        if (registration.ActiveHandlers == 0)
        {
          registration.PartitionTokens.Clear();
          registration.RevokedTokens.Clear();
          registration.RetiredTokens.Clear();
          disposeImmediately = true;
        }
      }

      if (disposeImmediately)
      {
        _activeRegistrations.TryRemove(registration.Id, out _);
        CancelAndDisposeSources(sources);
      }
      else
      {
        CancelSources(sources);
      }
    }
  }

  public sealed class ConsumerRegistration
  {
    internal ConsumerRegistration(Guid id, string groupId)
    {
      Id = id;
      GroupId = groupId;
    }

    public Guid Id { get; }
    public string GroupId { get; }
    internal object Sync { get; } = new();
    internal Dictionary<TopicPartition, CancellationTokenSource> PartitionTokens { get; } = new();
    internal Dictionary<TopicPartition, CancellationToken> RevokedTokens { get; } = new();
    internal List<CancellationTokenSource> RetiredTokens { get; } = new();
    internal int ActiveHandlers;
    internal bool DisposeWhenIdle;
  }
}