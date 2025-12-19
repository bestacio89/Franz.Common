namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

using System.Collections.Concurrent;
using System.Threading.Tasks;

public static class MultiHandlerProbe
{
  private static readonly ConcurrentBag<string> _received = new();
  private static TaskCompletionSource<bool> _tcs = new();

  public static void Reset()
  {
    _received.Clear();
    _tcs = new TaskCompletionSource<bool>();
  }

  public static void Hit(string value)
  {
    _received.Add(value);

    if (_received.Count >= 2)
      _tcs.TrySetResult(true);
  }

  public static Task WaitAsync(TimeSpan timeout)
    => _tcs.Task.WaitAsync(timeout);

  public static int Count => _received.Count;
}
