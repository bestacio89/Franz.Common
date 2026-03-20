namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public static class MultiHandlerProbe
{
  private static int _count;
  private static int _expected;
  private static TaskCompletionSource<bool> _tcs = new();

  public static int Count => _count;

  public static void Reset(int expected = 2)
  {
    _count = 0;
    _expected = expected;
    _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
  }

  public static void MarkHandled()
  {
    var current = Interlocked.Increment(ref _count);
    if (current >= _expected)
    {
      _tcs.TrySetResult(true);
    }
  }

  // Fix: Ensure the signature matches your test call
  public static Task WaitAsync(int expectedCount, TimeSpan timeout)
  {
    _expected = expectedCount;
    // If we already reached the count before waiting
    if (_count >= _expected) return Task.CompletedTask;

    return _tcs.Task.WaitAsync(timeout);
  }
}