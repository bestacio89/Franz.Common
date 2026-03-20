using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public interface ITestProbe
{
  // The test will await this task
  Task CompletionTask { get; }
  bool Handled { get; }
  void MarkHandled();
  void Reset();
}

