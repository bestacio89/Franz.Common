namespace Franz.Common.Messaging.Kafka;

internal class TopicPartitionAssignment
{
  public TopicPartitionAssignment()
  {
  }

  public int Partition { get; set; }
  public int[] Replicas { get; set; }
  public string Topic { get; set; }
  public Dictionary<string, object> CustomHeaders { get; set; }
}
