using System;
using System.Collections.Generic;
using System.Text;
#nullable enable

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Describes the outcome of a saga step.
/// </summary>
public enum SagaTransitionType
{
  /// <summary>
  /// No meaningful transition was produced.
  /// </summary>
  None = 0,

  /// <summary>
  /// The saga continues and emits a follow-up message
  /// (command or event) to be dispatched.
  /// </summary>
  Continue = 1,

  /// <summary>
  /// The saga completed successfully and can be closed.
  /// </summary>
  Complete = 2,

  /// <summary>
  /// The saga step should be retried after an optional delay.
  /// </summary>
  Retry = 3,

  /// <summary>
  /// The saga requests compensation to be executed
  /// (usually by emitting a compensating command).
  /// </summary>
  Compensate = 4,

  /// <summary>
  /// The saga failed permanently. The orchestrator should
  /// stop processing and route the error to the configured sink.
  /// </summary>
  Fail = 5
}
