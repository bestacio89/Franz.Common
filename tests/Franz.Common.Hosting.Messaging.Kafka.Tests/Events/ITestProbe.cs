using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Events;

public interface ITestProbe
{
  bool Handled { get; }
  void MarkHandled();
}

