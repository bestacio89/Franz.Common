#nullable enable
using Confluent.Kafka;

namespace Franz.Common.Messaging.Kafka.Tests; 

public static class KafkaTestUtils
{
  public static async Task WaitForTopicReadyAsync(
      string bootstrapServers,
      string topic,
      TimeSpan? timeout = null)
  {
    timeout ??= TimeSpan.FromSeconds(15);
    var deadline = DateTime.UtcNow + timeout.Value;

    var config = new ConsumerConfig
    {
      BootstrapServers = bootstrapServers,
      GroupId = $"probe-{Guid.NewGuid():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    };

    using var consumer = new ConsumerBuilder<string, string>(config).Build();

    consumer.Subscribe(topic);

    while (DateTime.UtcNow < deadline)
    {
      try
      {
        // 🔥 THIS is the key: polling drives assignment
        consumer.Consume(TimeSpan.FromMilliseconds(200));

        var assignment = consumer.Assignment;

        if (assignment != null && assignment.Count > 0)
        {
          return; // ✅ Topic fully ready
        }
      }
      catch (ConsumeException)
      {
        // transient during startup
      }
      catch (KafkaException)
      {
        // metadata not ready yet
      }

      await Task.Delay(200);
    }

    throw new TimeoutException(
      $"Kafka topic '{topic}' was not ready within {timeout.Value.TotalSeconds} seconds.");
  }
}