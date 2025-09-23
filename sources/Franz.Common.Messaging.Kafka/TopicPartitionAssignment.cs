namespace Franz.Common.Messaging.Kafka;

internal class TopicPartitionAssignment
{
  public TopicPartitionAssignment()
  {
    Replicas = Array.Empty<int>();
    Topic = string.Empty;
    CustomHeaders = new Dictionary<string, object>();
  }

  public int Partition { get; set; }
  public int[] Replicas { get; set; }
  public string Topic { get; set; }
  public Dictionary<string, object> CustomHeaders { get; set; }
}
