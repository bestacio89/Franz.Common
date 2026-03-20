using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Moq;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;

namespace Franz.Common.Messaging.Kafka.Tests.Modeling;

public class KafkaModelTests
{
  private static IConnectionProvider CreateConnectionProvider()
  {
    var producerConfig = new ProducerConfig
    {
      BootstrapServers = "localhost:9092"
    };

    var producer = new ProducerBuilder<string, object>(producerConfig).Build();

    var mock = new Mock<IConnectionProvider>();
    mock.Setup(x => x.Current).Returns(producer);

    return mock.Object;
  }

  [Fact]
  public void Constructor_Should_Not_Throw()
  {
    var connectionProvider = CreateConnectionProvider();

    Action act = () => new KafkaModel(connectionProvider);

    act.Should().NotThrow();
  }

  [Fact]
  public async Task Produce_Should_Throw_NotImplemented()
  {
    var connectionProvider = CreateConnectionProvider();
    var model = new KafkaModel(connectionProvider);

    Func<Task> act = async () =>
        await model.Produce("topic", new { Value = "test" }, CancellationToken.None);

    await act.Should().ThrowAsync<NotImplementedException>();
  }

  [Fact]
  public void Dispose_Should_Not_Throw()
  {
    var connectionProvider = CreateConnectionProvider();
    var model = new KafkaModel(connectionProvider);

    Action act = model.Dispose;

    act.Should().NotThrow();
  }
}