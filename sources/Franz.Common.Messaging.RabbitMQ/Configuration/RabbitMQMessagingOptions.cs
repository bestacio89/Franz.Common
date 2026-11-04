#nullable enable
using Franz.Common.Messaging.Configuration;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class RabbitMQMessagingOptions : MessagingOptions
{
  // --- Topology & Routing ---
  public string? ExchangeName { get; set; }
  public string? QueueName { get; set; }
  public string? DeadLetterExchangeName { get; set; }
  public string? DeadLetterQueueName { get; set; }
  public string? DefaultRoutingKey { get; set; }
  /// <summary>Replay strategy: "NoReplay" or "ProgressiveFourStep".</summary>
  public string ReplayStrategy { get; set; } = "NoReplay";

  /// <summary>
  /// Maximum number of deliveries processed concurrently by each RabbitMQ listener.
  /// Set to 1 to preserve sequential processing.
  /// </summary>
  public int MaxConcurrency { get; set; } = 1;
  // --- Connectivity ---
  public string? BootStrapServers { get; set; }   // URI-based

  // --- Optional Connection Overrides ---
  public bool? AutomaticRecoveryEnabled { get; set; } = true;
  public bool? TopologyRecoveryEnabled { get; set; } = true;
  public int? RequestedHeartbeatSeconds { get; set; } = 30;
}
