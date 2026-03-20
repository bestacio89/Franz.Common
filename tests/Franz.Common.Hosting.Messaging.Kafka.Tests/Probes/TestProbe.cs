using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public sealed class TestProbe : ITestProbe
{
  // Allows the test to 'Await' the result
  private TaskCompletionSource<string> _completionSource = new();

  public Task<string> ReceivedTask => _completionSource.Task;

  public void MarkHandled(string messageValue)
  {
    // Thread-safe signaling
    _completionSource.TrySetResult(messageValue);
  }

  public void Reset()
  {
    _completionSource = new();
  }
}