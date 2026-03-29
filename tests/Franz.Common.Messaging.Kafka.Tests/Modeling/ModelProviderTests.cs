#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Modeling;

[Collection("Kafka")]
public sealed class ModelProviderTests : IAsyncDisposable
{
  private readonly Mock<IConnectionFactoryProvider> _factoryProviderMock = new();
  private readonly KafkaContainerFixture _fixture;
  private readonly ModelProvider _sut;

  public ModelProviderTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture;
    _factoryProviderMock.Setup(x => x.Current).Returns(new ProducerConfig
    {
      BootstrapServers = _fixture.BootstrapServers
    });

    _sut = new ModelProvider(_factoryProviderMock.Object);
  }

  [Fact]
  public void Current_ShouldReturnSameInstance_OnMultipleAccesses()
  {
    var first = _sut.Current;
    var second = _sut.Current;

    first.Should().NotBeNull();
    second.Should().BeSameAs(first);
    _factoryProviderMock.Verify(x => x.Current, Times.Once);
  }

  [Fact]
  public async Task Current_UnderHighLoad_ShouldEnsureOnlyOneModelCreated()
  {
    var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() => _sut.Current));
    var results = await Task.WhenAll(tasks);

    var primary = results[0];
    results.Should().AllSatisfy(r => r.Should().BeSameAs(primary));
    _factoryProviderMock.Verify(x => x.Current, Times.Once);
  }

  [Fact]
  public async Task DisposeAsync_ShouldTriggerInternalModelCleanup()
  {
    var model = _sut.Current;
    await _sut.DisposeAsync();
    Assert.Throws<ObjectDisposedException>(() => _sut.Current);
  }

  [Fact]
  public async Task DisposeAsync_ShouldWait_IfInitializationIsInProgress()
  {
    // SENIOR FIX: Never use localhost:9092. It creates "Zombie Threads".
    // Use the real fixture address so the native library finds a valid port.
    var initSignal = new TaskCompletionSource<bool>();
    var releaseSignal = new TaskCompletionSource<bool>();

    _factoryProviderMock.Setup(x => x.Current).Callback(() =>
    {
      initSignal.SetResult(true);
      releaseSignal.Task.Wait();
    }).Returns(new ProducerConfig { BootstrapServers = _fixture.BootstrapServers });

    var initTask = Task.Run(() => _sut.Current);
    await initSignal.Task;

    var disposeTask = _sut.DisposeAsync().AsTask();
    disposeTask.IsCompleted.Should().BeFalse("Disposal must wait for the init lock.");

    releaseSignal.SetResult(true);
    await Task.WhenAll(initTask, disposeTask);

    Assert.Throws<ObjectDisposedException>(() => _sut.Current);
  }

  public async ValueTask DisposeAsync() => await _sut.DisposeAsync();
}