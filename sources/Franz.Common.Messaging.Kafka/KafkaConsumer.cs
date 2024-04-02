using Confluent.Kafka;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System;
using System.Reflection.Metadata;
using Handle = Confluent.Kafka.Handle;

namespace Franz.Common.Messaging.Kafka;

/*
Using this class, you can create a Kafka consumer in the following way:
var config = new ConsumerConfig
{
BootstrapServers = "localhost:9092",
GroupId = "my-group",
AutoOffsetReset = AutoOffsetReset.Earliest
};
using (var consumer = new KafkaConsumer(config))
{
consumer.Subscribe("my-topic");
while (true)
{

var result = consumer.Consume(TimeSpan.FromSeconds(1));

if (result != null)

  Console.WriteLine($"Consumed message '{result.Value}' at: '{result.TopicPartitionOffset}'.");
}
}
*/
public class KafkaConsumer : IConsumer<string, string>
{
  private readonly IConsumer<string, string> _consumer;

  public KafkaConsumer(IConsumer<string, string> consumer)
  {
    _consumer = consumer;
  }

  public ConsumerConfig Config => _consumer.Config;

  public void Assign(TopicPartition topicPartition) => _consumer.Assign(topicPartition);

  public void Assign(List<TopicPartition> topicPartitions) => _consumer.Assign(topicPartitions);

  public void Unassign() => _consumer.Unassign();

  public void Subscribe(string topic) => _consumer.Subscribe(topic);

  public void Subscribe(IEnumerable<string> topics) => _consumer.Subscribe(topics);

  public void Unsubscribe() => _consumer.Unsubscribe();

  public List<TopicPartition> Assignment => _consumer.Assignment;

  public List<string> Subscription => _consumer.Subscription;

  public string MemberId => _consumer.MemberId;

  public IConsumerGroupMetadata ConsumerGroupMetadata => _consumer.ConsumerGroupMetadata;

  public Handle Handle => _consumer.Handle;

  public string Name => _consumer.Name;

  public void Dispose() => _consumer.Dispose();

  public ConsumeResult<string, string> Consume(TimeSpan timeout) => _consumer.Consume(timeout);

  public async Task<ConsumeResult<string, string>> ConsumeAsync(CancellationToken cancellationToken, TimeSpan timeout)
  {
    try
    {
      // Use Confluent.Kafka's ConsumeAsync with cancellation token (if available)
      if (_consumer.TryConsumeAsync(out ConsumeResult<string, string> consumeResult, cancellationToken, timeout).Result)
      {
        return consumeResult;
      }
      else
      {
        // Handle timeout scenario (e.g., throw exception or log)
        throw new Exception("Consume operation timed out."); // Example exception
      }
    }
    catch (ConsumeException ex)
    {
      // Handle other potential exceptions
      throw;
    }
  }

  public async Task<bool> TryConsumeAsync(out ConsumeResult<TKey, TValue> result, CancellationToken cancellationToken, TimeSpan timeout)
  {
    try
    {
      // Check if Confluent.Kafka supports TryConsumeAsync (v1.0.0 or later)
      if (_consumer.TryConsumeAsync(out result, cancellationToken, timeout))
      {
        return true;
      }
      else
      {
        // Handle timeout scenario (e.g., throw exception or log)
        return false;
      }
    }
    catch (ConsumeException ex)
    {
      // Handle other potential exceptions
      throw;
    }
  }

  public ConsumeResult<string, string> Consume(int millisecondsTimeout) => _consumer.Consume(millisecondsTimeout);

  public void Assign(TopicPartitionOffset partition) => _consumer.Assign(partition);

  public void Assign(IEnumerable<TopicPartitionOffset> partitions) => _consumer.Assign(partitions);

  public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions) => _consumer.IncrementalAssign(partitions);

  public void IncrementalAssign(IEnumerable<TopicPartition> partitions) => _consumer.IncrementalAssign(partitions);

  public void IncrementalUnassign(IEnumerable<TopicPartition> partitions) => _consumer.IncrementalUnassign(partitions);

  public void StoreOffset(ConsumeResult<string, string> result) => _consumer.StoreOffset(result);

  public void StoreOffset(TopicPartitionOffset offset) => _consumer.StoreOffset(offset);

  public List<TopicPartitionOffset> Commit() => _consumer.Commit();

  public void Commit(IEnumerable<TopicPartitionOffset> offsets) => _consumer.Commit(offsets);

  public void Commit(ConsumeResult<string, string> result) => _consumer.Commit(result);

  public void Seek(TopicPartitionOffset tpo) => _consumer.Seek(tpo);

  public void Pause(IEnumerable<TopicPartition> partitions) => _consumer.Pause(partitions);

  public void Resume(IEnumerable<TopicPartition> partitions) => _consumer.Resume(partitions);

  public List<TopicPartitionOffset> Committed(TimeSpan timeout) => _consumer.Committed(timeout).ToList();

  public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout) => _consumer.Committed(partitions, timeout).ToList();

  public Offset Position(TopicPartition partition) => _consumer.Position(partition);

  public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout) => _consumer.OffsetsForTimes(timestampsToSearch, timeout);

  public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) => _consumer.GetWatermarkOffsets(topicPartition);

  public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) => _consumer.QueryWatermarkOffsets(topicPartition, timeout);

  public void Close() => _consumer.Close();

  public int AddBrokers(string brokers) => _consumer.AddBrokers(brokers);
}

