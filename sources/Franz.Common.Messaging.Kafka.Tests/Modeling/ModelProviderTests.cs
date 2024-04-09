using System;
using Moq;
using Xunit;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Testing;

namespace Franz.Common.Messaging.Kafka.Modeling.Tests
{
  public class ModelProviderTests
  {
    private readonly Mock<IConnectionProvider> _connectionProviderMock;
    private readonly ModelProvider _modelProvider;

    public ModelProviderTests()
    {
      _connectionProviderMock = new Mock<IConnectionProvider>();
      _modelProvider = new ModelProvider(_connectionProviderMock.Object);
    }

    [Fact]
    public void Current_ShouldReturnKafkaModel()
    {
      var model = _modelProvider.Current;

      Assert.IsType<KafkaModel>(model);  // Use IsType for type checking
    }

    [Fact]
    public void Current_ShouldReturnSameInstance()
    {
      var model1 = _modelProvider.Current;
      var model2 = _modelProvider.Current;

      Assert.Same(model1, model2);  // Use Same for reference equality
    }

    [Fact]
    public void Dispose_ShouldDisposeModel()
    {
      var model = _modelProvider.Current;
      var modelMock = Moq.Mock.Get<IDisposable>(model);  // Cast to IDisposable

      _modelProvider.Dispose();

      modelMock.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldNotDisposeModelWhenModelIsNotCreated()
    {
      _modelProvider.Dispose();

      _connectionProviderMock.Verify(x => x.Current.Dispose(), Times.Never);
    }
  }
}
