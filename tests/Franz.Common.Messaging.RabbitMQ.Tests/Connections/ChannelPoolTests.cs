#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Connections;

[Collection(nameof(RabbitMqTestCollection))]
public sealed class ChannelPoolIntegrationTests
{
  private readonly RabbitMqContainerFixture _fixture;

  public ChannelPoolIntegrationTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task GetAsync_ShouldCreateNewChannel_WhenPoolIsEmpty()
  {
    // Arrange
    var connectionProvider = _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>();
    var pool = new ChannelPool(connectionProvider);

    // Act
    var channel = await pool.GetAsync();

    // Assert
    channel.Should().NotBeNull();
    channel.IsOpen.Should().BeTrue();
  }

  [Fact]
  public async Task Return_ShouldReuseChannel_WhenPoolIsNotFull()
  {
    // Arrange
    var connectionProvider = _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>();
    var pool = new ChannelPool(connectionProvider);

    // Act
    var channel1 = await pool.GetAsync();
    pool.Return(channel1);
    var channel2 = await pool.GetAsync();

    // Assert
    channel2.Should().BeSameAs(channel1); // real reference reuse
    channel2.IsOpen.Should().BeTrue();
  }
  [Fact]
  public async Task GetPublisherConfirmChannelAsync_ShouldConfirmRoutableMandatoryPublish()
  {
    var connectionProvider = _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>();
    await using var pool = new ChannelPool(connectionProvider);
    await using var channel = await pool.GetPublisherConfirmChannelAsync();
    var queueName = $"confirm-{Guid.NewGuid():N}";

    await channel.QueueDeclareAsync(
        queue: queueName,
        durable: false,
        exclusive: true,
        autoDelete: true);

    await channel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: queueName,
        mandatory: true,
        basicProperties: new RabbitMQ.Client.BasicProperties(),
        body: new ReadOnlyMemory<byte>([1, 2, 3]));

    channel.IsOpen.Should().BeTrue();
  }

  [Fact]
  public async Task GetPublisherConfirmChannelAsync_ShouldFailForUnroutableMandatoryPublish()
  {
    var connectionProvider = _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>();
    await using var pool = new ChannelPool(connectionProvider);
    await using var channel = await pool.GetPublisherConfirmChannelAsync();

    var act = async () => await channel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: $"missing-{Guid.NewGuid():N}",
        mandatory: true,
        basicProperties: new RabbitMQ.Client.BasicProperties(),
        body: new ReadOnlyMemory<byte>([1, 2, 3]));

    await act.Should().ThrowAsync<RabbitMQ.Client.Exceptions.PublishException>();
    channel.IsOpen.Should().BeTrue();
  }

  [Fact]
  public async Task GetTransactionalChannelAsync_ShouldCreateChannelWithoutPublisherConfirmMode()
  {
    var connectionProvider = _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>();
    await using var pool = new ChannelPool(connectionProvider);
    await using var channel = await pool.GetTransactionalChannelAsync();

    channel.IsOpen.Should().BeTrue();
    await channel.TxSelectAsync();
    await channel.TxRollbackAsync();
  }

}