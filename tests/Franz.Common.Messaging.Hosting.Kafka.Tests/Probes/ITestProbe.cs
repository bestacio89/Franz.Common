using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;

/// <summary>
/// Correlation-aware probe for validating asynchronous Kafka message delivery.
/// Uses GuidV7 tracking to prevent cross-test interference.
/// </summary>
public interface ITestProbe
{
  /// <summary>
  /// Awaits the arrival of a specific message ID within the given timeout.
  /// </summary>
  Task<bool> WaitForArrivalAsync(Guid messageId, TimeSpan timeout = default);

  /// <summary>
  /// Signals that a message has been successfully processed by a handler.
  /// </summary>
  void SignalArrival(Guid messageId);
  void SignalFailure(Guid messageId, Exception exception);
  /// <summary>
  /// Clears all tracked messages.
  /// </summary>
  void Reset();
}



