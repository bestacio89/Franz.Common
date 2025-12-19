using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public sealed class TestProbe : ITestProbe
{
  public bool Handled { get; private set; }

  public void MarkHandled()
  {
    Handled = true;
  }
}
