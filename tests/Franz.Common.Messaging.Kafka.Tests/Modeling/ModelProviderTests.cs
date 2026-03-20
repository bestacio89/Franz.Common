using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Moq;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;

namespace Franz.Common.Messaging.Kafka.Tests.Modeling;

public class ModelProviderTests
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
  public void Current_Should_Lazily_Create_Model()
  {
    var connectionProvider = CreateConnectionProvider();
    var provider = new ModelProvider(connectionProvider);

    var model = provider.Current;

    model.Should().NotBeNull();
  }

  [Fact]
  public void Current_Should_Return_Same_Instance()
  {
    var connectionProvider = CreateConnectionProvider();
    var provider = new ModelProvider(connectionProvider);

    var first = provider.Current;
    var second = provider.Current;

    first.Should().BeSameAs(second);
  }

  [Fact]
  public void Dispose_Should_Dispose_Model()
  {
    var connectionProvider = CreateConnectionProvider();
    var provider = new ModelProvider(connectionProvider);

    var model = provider.Current;

    provider.Dispose();

    Action act = () => model.Dispose();

    act.Should().Throw<ObjectDisposedException>();
  }
}