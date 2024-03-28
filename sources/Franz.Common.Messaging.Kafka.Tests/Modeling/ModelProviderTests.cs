using Franz.Common.Messaging.Kafka.Connections;
using Moq;
using System;
using NUnit.Framework;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Testing;

namespace Franz.Common.Messaging.Kafka.Modeling.Tests
{
  [TestFixture]
  public class ModelProviderTests:UnitTest
  {
    private readonly Mock<IConnectionProvider> _connectionProviderMock;
    private readonly ModelProvider _modelProvider;

    public ModelProviderTests()
    {
      _connectionProviderMock = new Mock<IConnectionProvider>();
      _modelProvider = new ModelProvider(_connectionProviderMock.Object);
    }

    [Test]
    public void Current_ShouldReturnKafkaModel()
    {
      var model = _modelProvider.Current;

      Assert.IsInstanceOf<KafkaModel>(model);
    }

    [Test]
    public void Current_ShouldReturnSameInstance()
    {
      var model1 = _modelProvider.Current;
      var model2 = _modelProvider.Current;

      Assert.IsTrue(model1.Equals(model2));
    }

    [Test]
    public void Dispose_ShouldDisposeModel()
    {
      var model = _modelProvider.Current;
      var modelMock = Mock.Get(model);

      _modelProvider.Dispose();

      modelMock.Verify(x => x.Dispose(), Times.Once);
    }

    [Test]
    public void Dispose_ShouldNotDisposeModelWhenModelIsNotCreated()
    {
      _modelProvider.Dispose();

      _connectionProviderMock.Verify(x => x.Current.Dispose(), Times.Never);
    }
  }
}
