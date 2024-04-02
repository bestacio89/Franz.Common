namespace Franz.Common.Annotations;

public class RequiredKafkaTopicAttribute : Attribute
{
  public string Topic { get; }
  public string DeadLetterTopic { get; set; }
  public string Format { get; }  // New optional parameter

  public RequiredKafkaTopicAttribute(string format, string entityName)
  {
    Format = format;
    Topic = string.Format(format, entityName);  // Use format string
  }
}