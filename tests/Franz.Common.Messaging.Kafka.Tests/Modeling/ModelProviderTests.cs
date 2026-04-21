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
  }

  [Fact]
  public void Current_ShouldBeThreadSafe_AndReturnConsistentInstance()
  {
    var results = Enumerable.Range(0, 50)
      .AsParallel()
      .Select(_ => _sut.Current)
      .ToList();

    var primary = results[0];

    results.Should().AllSatisfy(r => r.Should().BeSameAs(primary));
  }

  [Fact]
  public async Task DisposeAsync_ShouldMakeProviderUnavailable()
  {
    var model = _sut.Current;

    await _sut.DisposeAsync();

    Action act = () => _ = _sut.Current;

    act.Should().Throw<ObjectDisposedException>();
  }

  [Fact]
  public async Task DisposeAsync_ShouldBeIdempotent()
  {
    await _sut.DisposeAsync();
    await _sut.DisposeAsync();

    // no exception expected
    true.Should().BeTrue();
  }

  public async ValueTask DisposeAsync()
  {
    await _sut.DisposeAsync();
  }
}