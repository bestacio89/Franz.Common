using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.AzureEventBus.Configuration;

public sealed class AzureEventBusDeadLetterOptions
{
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// DLQ on deserialization failures (poison payload).
  /// </summary>
  public bool DeadLetterOnDeserializationError { get; set; } = true;

  /// <summary>
  /// DLQ on validation errors (contract/semantic invalid).
  /// </summary>
  public bool DeadLetterOnValidationError { get; set; } = true;
}
