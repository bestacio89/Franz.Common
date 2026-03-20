using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public sealed class TestPipelineProbe : ITestPipelineProbe
{
  public bool WasExecuted { get; private set; }

  public void MarkExecuted() => WasExecuted = true;
  public void Reset() => WasExecuted = false;
}