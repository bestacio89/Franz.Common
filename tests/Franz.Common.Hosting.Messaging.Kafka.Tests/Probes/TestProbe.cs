using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public sealed class TestProbe : ITestProbe
{
  private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public Task CompletionTask => _tcs.Task;
  public bool Handled => _tcs.Task.IsCompletedSuccessfully;
  public void MarkHandled() => _tcs.TrySetResult(true);
  public void Reset() => _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
}