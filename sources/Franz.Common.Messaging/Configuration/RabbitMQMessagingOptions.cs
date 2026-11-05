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
  /// <summary>Controls whether failed deliveries are dead-lettered immediately or routed through progressive retry queues.</summary>
  public RabbitMQReplayStrategy ReplayStrategy { get; set; } = RabbitMQReplayStrategy.NoReplay;

  /// <summary>
  /// Maximum number of deliveries processed concurrently by each RabbitMQ listener.
  /// Set to 1 to preserve sequential processing.
  /// </summary>
  public int MaxConcurrency { get; set; } = 1;

  /// <summary>Controls whether topology is created, verified, or left untouched.</summary>
  public ProvisioningMode ProvisioningMode { get; set; } = ProvisioningMode.Create;

  /// <summary>Whether application queues are durable.</summary>
  public bool QueueDurable { get; set; } = true;

  /// <summary>RabbitMQ queue implementation used for application, dead-letter, and retry queues.</summary>
  public RabbitMQQueueType QueueType { get; set; } = RabbitMQQueueType.Quorum;
  public MessageFailurePolicy? FailurePolicy { get; set; }
  public MessageOrderingStrategy? OrderingStrategy { get; set; }

  public int? MessageTtlMilliseconds { get; set; }
  public long? MaxLength { get; set; }
  public long? MaxLengthBytes { get; set; }
  public int? DeliveryLimit { get; set; }

  /// <summary>
  /// RabbitMQ Management HTTP API base URI, required when ProvisioningMode is Verify
  /// so the initializer can inspect bindings (AMQP has no passive binding-query operation).
  /// </summary>
  public string? ManagementApiUrl { get; set; }

  /// <summary>Optional Management API credentials. When omitted, AMQP connection credentials are reused.</summary>
  public string? ManagementApiUsername { get; set; }
  public string? ManagementApiPassword { get; set; }

  /// <summary>Management API virtual host. When omitted, the AMQP virtual host is used.</summary>
  public string? ManagementApiVirtualHost { get; set; }

  // --- Connectivity ---
  public string? BootStrapServers { get; set; }   // URI-based

  // --- Optional Connection Overrides ---
  public bool? AutomaticRecoveryEnabled { get; set; } = true;
  public bool? TopologyRecoveryEnabled { get; set; } = true;
  public int? RequestedHeartbeatSeconds { get; set; } = 30;


 
}
public enum RabbitMQQueueType
{
  Quorum,
  Classic
}


public enum RabbitMQReplayStrategy
{
  NoReplay = 0,
  ProgressiveFourStep = 1
}
