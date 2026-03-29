using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;

public interface ITestPipelineProbe
{
  void RecordScope(Guid messageId, Guid scopeId);
  void RecordDispose(Guid scopeId);
  IEnumerable<Guid> GetCapturedScopes();
  int GetDisposeCount();
  Task<bool> WaitForMessagesAsync(int count, TimeSpan timeout);
}