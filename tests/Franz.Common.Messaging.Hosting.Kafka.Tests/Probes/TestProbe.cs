#nullable enable
using System.Collections.Concurrent;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;

public sealed class TestProbe : ITestProbe
{
  private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _completions = new();
  private readonly ConcurrentDictionary<Guid, Exception> _errors = new();
  private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(15);

  /// <summary>
  /// Awaits the arrival of a specific GuidV7 message. 
  /// Returns true if successful, false on timeout, and throws if the handler signaled a failure.
  /// </summary>
  public async Task<bool> WaitForArrivalAsync(Guid messageId, TimeSpan timeout = default)
  {
    var tcs = _completions.GetOrAdd(messageId, _ =>
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

    using var cts = new CancellationTokenSource(timeout == default ? _defaultTimeout : timeout);

    try
    {
      using (cts.Token.Register(() => tcs.TrySetResult(false)))
      {
        var result = await tcs.Task;

        // If the handler signaled a failure, throw it so the test fails with the actual exception
        if (_errors.TryGetValue(messageId, out var ex))
        {
          throw ex;
        }

        return result;
      }
    }
    catch (OperationCanceledException)
    {
      return false;
    }
  }

  /// <summary>
  /// Signals that the MessagingHostedService successfully finished the strategy execution.
  /// </summary>
  public void SignalArrival(Guid messageId)
  {
    var tcs = _completions.GetOrAdd(messageId, _ =>
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

    tcs.TrySetResult(true);
  }

  /// <summary>
  /// Signals that the handler or executor failed, allowing the test to assert on the exception.
  /// </summary>
  public void SignalFailure(Guid messageId, Exception exception)
  {
    _errors.TryAdd(messageId, exception);
    var tcs = _completions.GetOrAdd(messageId, _ =>
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

    // Complete the task so the awaiter wakes up, but result is false (or exception is thrown above)
    tcs.TrySetResult(false);
  }

  public void Reset()
  {
    _completions.Clear();
    _errors.Clear();
  }
}