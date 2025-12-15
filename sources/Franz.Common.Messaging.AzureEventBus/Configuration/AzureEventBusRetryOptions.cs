using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.AzureEventBus.Configuration;

public sealed class AzureEventBusRetryOptions
{
  /// <summary>
  /// If you want adapter-level retry policies (Polly) around handler execution.
  /// Azure Service Bus already retries delivery; this is for transient handler/infra issues.
  /// </summary>
  public bool EnableHandlerResilience { get; set; } = true;

  /// <summary>
  /// Maximum number of handler-level retries (not delivery retries).
  /// </summary>
  public int MaxHandlerRetries { get; set; } = 2;

  public TimeSpan HandlerRetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}
