namespace Franz.Common.Annotations;

public class RequiredKafkaTopicAttribute : Attribute
{
  public string Topic { get; }
  public string? DeadLetterTopic { get; set; }   // optional
  public string Format { get; }

  public RequiredKafkaTopicAttribute(string format, string entityName)
  {
    Format = format ?? throw new ArgumentNullException(nameof(format));
    Topic = string.Format(format, entityName);
  }
}