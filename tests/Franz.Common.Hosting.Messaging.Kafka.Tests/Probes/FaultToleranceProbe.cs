namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

using System.Threading.Tasks;

public static class FaultToleranceProbe
{
  private static TaskCompletionSource<string> _tcs = new();

  public static void Reset()
    => _tcs = new TaskCompletionSource<string>();

  public static void Hit(string value)
    => _tcs.TrySetResult(value);

  public static Task<string> WaitAsync(TimeSpan timeout)
    => _tcs.Task.WaitAsync(timeout);
}
