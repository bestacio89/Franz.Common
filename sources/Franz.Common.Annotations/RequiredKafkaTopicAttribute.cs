#nullable enable
namespace Franz.Common.Annotations;

/// <summary>
/// Defines Kafka topic requirements for a controller or service.
/// Senior Note: Updated to support both dynamic formatting and explicit topic naming.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequiredKafkaTopicAttribute : Attribute
{
  // SENIOR FIX: Properties must have setters to support named arguments in attributes
  public string? Topic { get; set; }
  public string? Format { get; set; }
  public string? DeadLetterTopic { get; set; }

  /// <summary>
  /// Default constructor for named-parameter initialization:
  /// [RequiredKafkaTopic(Topic = "explicit-name")]
  /// </summary>
  public RequiredKafkaTopicAttribute() { }

  /// <summary>
  /// Constructor for simple format initialization:
  /// [RequiredKafkaTopic("{0}-in")]
  /// </summary>
  public RequiredKafkaTopicAttribute(string format)
  {
    Format = format ?? throw new ArgumentNullException(nameof(format));
  }

  /// <summary>
  /// Legacy/Greedy constructor for full initialization:
  /// [RequiredKafkaTopic("{0}-in", "Users")]
  /// </summary>
  public RequiredKafkaTopicAttribute(string format, string entityName)
  {
    Format = format ?? throw new ArgumentNullException(nameof(format));
    Topic = string.Format(format, entityName);
  }
}