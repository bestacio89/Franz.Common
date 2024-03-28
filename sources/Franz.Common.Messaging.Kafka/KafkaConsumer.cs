using Confluent.Kafka;
using System;

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
  public readonly IConsumer<string, string> _consumer;
  public readonly ConsumerConfig _config;

  public KafkaConsumer(IConsumer<string, string> innerConsumer, ConsumerConfig config)
  {
    _consumer = innerConsumer;
    _config = config;
  }

  public KafkaConsumer(KafkaConsumer kafkaConsumer)
  {
  }

  public ConsumerConfig Config
  {
    get { return _config; }
  }
  public void Assign(TopicPartition topicPartition)
  {
    _consumer.Assign(topicPartition);
  }

  public void Assign(List<TopicPartition> topicPartitions)
  {
    _consumer.Assign(topicPartitions);
  }

  public void Unassign()
  {
    _consumer.Unassign();
  }

  public void Subscribe(string topic)
    {
      _consumer.Subscribe(topic);
    }

  public void Subscribe(IEnumerable<string> topics)
  {
    _consumer.Subscribe(topics);
  }

  public void Unsubscribe()
  {
    _consumer.Unsubscribe();
  }

  public List<TopicPartition> Assignment
  {
    get
    {
      return _consumer.Assignment;
    }
  }

  public List<string> Subscription
  {
    get
    {
      return _consumer.Subscription;
    }
  }
  public string MemberId
  {
    get { return _consumer.MemberId; }
  }

  public IConsumerGroupMetadata ConsumerGroupMetadata
  {
    get { return _consumer.ConsumerGroupMetadata; }
  }

  public Handle Handle
  {
    get { return _consumer.Handle; }
  }

  public string Name
  {
    get { return _consumer.Name; }
  }
  public void Dispose()
    {
      _consumer.Dispose();
    }

  public ConsumeResult<string, string> Consume(TimeSpan timeout)
  {
    return _consumer.Consume(timeout);
  }

  public ConsumeResult<string, string> Consume(CancellationToken cancellationToken)
  {
    return _consumer.Consume(cancellationToken) ;
  }



  public ConsumeResult<string, string> Consume(int millisecondsTimeout)
  {
    return _consumer.Consume(millisecondsTimeout);
  }

  ConsumeResult<string, string> IConsumer<string, string>.Consume(CancellationToken cancellationToken)
  {
    return _consumer.Consume(cancellationToken);
  }

  ConsumeResult<string, string> IConsumer<string, string>.Consume(TimeSpan timeout)
  {
    return _consumer.Consume(timeout);
  }

  public void Assign(TopicPartitionOffset partition)
  {
    _consumer.Assign(partition);
  }

  public void Assign(IEnumerable<TopicPartitionOffset> partitions)
  {
    _consumer.Assign(partitions);
  }

  public void Assign(IEnumerable<TopicPartition> partitions)
  {
    _consumer.Assign(partitions);
  }

  public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions)
  {
    _consumer.Assign(partitions);
  }

  public void IncrementalAssign(IEnumerable<TopicPartition> partitions)
  {
    _consumer.Assign(partitions);
  }

  public void IncrementalUnassign(IEnumerable<TopicPartition> partitions)
  {
    _consumer.Assign(partitions);
  }

  public void StoreOffset(ConsumeResult<string, string> result)
  {
    _consumer.StoreOffset(result);
  }

  public void StoreOffset(TopicPartitionOffset offset)
  {
    _consumer.StoreOffset(offset);
  }

  public List<TopicPartitionOffset> Commit()
  {
    return _consumer.Commit();
  }

  public void Commit(IEnumerable<TopicPartitionOffset> offsets)
  {
    _consumer.Commit(offsets);
  }

  public void Commit(ConsumeResult<string, string> result)
  {
     _consumer.Commit(result);  
  }

  public void Seek(TopicPartitionOffset tpo)
  {
    _consumer.Seek(tpo);
  }

  public void Pause(IEnumerable<TopicPartition> partitions)
  {
    _consumer.Pause(partitions);
  }

  public void Resume(IEnumerable<TopicPartition> partitions)
  {
    _consumer.Resume(partitions);
  }

  public List<TopicPartitionOffset> Committed(TimeSpan timeout)
  {
    return _consumer.Committed(timeout).ToList();
  }

  public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout)
  {
    return _consumer.Committed(partitions, timeout).ToList();
  }

  public Offset Position(TopicPartition partition)
  {
   return  _consumer.Position(partition);
  }

  public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout)
  {
    return _consumer.OffsetsForTimes(timestampsToSearch,timeout);
  }

  public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition)
  {
    return GetWatermarkOffsets(topicPartition);
  }

  public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout)
  {
    return _consumer.QueryWatermarkOffsets(topicPartition, timeout);
  }

  public void Close()
  {
    _consumer.Close();
  }

  public int AddBrokers(string brokers)
  {
    return _consumer.AddBrokers(brokers);
  }
}
