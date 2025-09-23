namespace Franz.Common.Messaging.Kafka;

internal class TopicPartitionAssignment
{
    public int Partition { get; set; }
    public int[] Replicas { get; set; } = Array.Empty<int>();   // never null
    public string Topic { get; set; } = string.Empty;           // never null
    public Dictionary<string, object> CustomHeaders { get; set; } = new(); // never null
}
