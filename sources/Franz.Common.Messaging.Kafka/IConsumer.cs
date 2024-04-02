using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
namespace Franz.Common.Messaging.Kafka;
public interface IConsumer<TKey, TValue>
{
  ConsumerConfig Config { get; }

  void Assign(TopicPartition topicPartition);
  void Assign(List<TopicPartition> topicPartitions);
  void Unassign();

  void Subscribe(string topic);
  void Subscribe(IEnumerable<string> topics);
  void Unsubscribe();

  List<TopicPartition> Assignment { get; }
  List<string> Subscription { get; }

  string MemberId { get; }
  IConsumerGroupMetadata ConsumerGroupMetadata { get; }
    Confluent.Kafka.Handle Handle { get; }
  string Name { get; }

  void Dispose();

  ConsumeResult<TKey, TValue> Consume(TimeSpan timeout);
  ConsumeResult<TKey, TValue> Consume();
  ConsumeResult<TKey, TValue> Consume(int millisecondsTimeout);

  void Assign(TopicPartitionOffset partition);
  void Assign(IEnumerable<TopicPartitionOffset> partitions);
  void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions);
  void IncrementalAssign(IEnumerable<TopicPartition> partitions);
  void IncrementalUnassign(IEnumerable<TopicPartition> partitions);

  void StoreOffset(ConsumeResult<TKey, TValue> result);
  void StoreOffset(TopicPartitionOffset offset);

  List<TopicPartitionOffset> Commit();
  void Commit(IEnumerable<TopicPartitionOffset> offsets);
  void Commit(ConsumeResult<TKey, TValue> result);

  void Seek(TopicPartitionOffset tpo);

  void Pause(IEnumerable<TopicPartition> partitions);
  void Resume(IEnumerable<TopicPartition> partitions);

  List<TopicPartitionOffset> Committed(TimeSpan timeout);
  List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout);

  Offset Position(TopicPartition partition);

  List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout);

  WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition);
  WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout);

  void Close();

  int AddBrokers(string brokers);
}
