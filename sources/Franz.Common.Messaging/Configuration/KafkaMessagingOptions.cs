#nullable enable
namespace Franz.Common.Messaging.Configuration;

/// <summary>
/// Kafka-specific messaging options.
/// </summary>
public sealed class KafkaMessagingOptions : MessagingOptions
{
  public string? BootStrapServers { get; set; }
  public string? GroupID { get; set; }
  public string? TopicName { get; set; }
  public string? DeadLetterTopicName { get; set; }
  public int? Partitions { get; set; }
  public short? ReplicationFactor { get; set; }
}