using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public interface ITestPipelineProbe
{
  bool WasExecuted { get; }
  void MarkExecuted();
  void Reset();
}
