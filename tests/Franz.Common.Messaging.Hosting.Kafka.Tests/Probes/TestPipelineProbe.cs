using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using System.Collections.Concurrent;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;

public sealed class TestPipelineProbe : ITestPipelineProbe
{
  private readonly ConcurrentDictionary<Guid, Guid> _messageToScope = new();
  private readonly ConcurrentBag<Guid> _disposedScopes = new();
  private readonly TaskCompletionSource<bool> _thresholdReached = new();
  private int _targetCount;
  private int _currentCount;

  public void RecordScope(Guid messageId, Guid scopeId)
  {
    _messageToScope.TryAdd(messageId, scopeId);
    if (Interlocked.Increment(ref _currentCount) >= _targetCount)
    {
      _thresholdReached.TrySetResult(true);
    }
  }

  public void RecordDispose(Guid scopeId) => _disposedScopes.Add(scopeId);

  public IEnumerable<Guid> GetCapturedScopes() => _messageToScope.Values;

  public int GetDisposeCount() => _disposedScopes.Count;

  public async Task<bool> WaitForMessagesAsync(int count, TimeSpan timeout)
  {
    _targetCount = count;
    if (_currentCount >= count) return true;

    using var cts = new CancellationTokenSource(timeout);
    using (cts.Token.Register(() => _thresholdReached.TrySetResult(false)))
    {
      return await _thresholdReached.Task;
    }
  }
}