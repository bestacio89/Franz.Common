#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Hosting;

public sealed class RabbitMqConsumerTests : IDisposable
{
  private readonly Mock<IChannel> _channelMock = new();
  private readonly string _testQueue = "franz.test.queue";
  private readonly ushort _testPrefetch = 25;

  public void Dispose() => _channelMock.Object.Dispose();

  [Fact]
  public async Task ConsumeAsync_ShouldYieldMessages_WhenEventsAreRaised()
  {
    // Arrange
    var sut = new RabbitMqConsumer(_channelMock.Object, _testQueue, _testPrefetch);
    IAsyncBasicConsumer? capturedConsumer = null;

    _channelMock.Setup(c => c.BasicConsumeAsync(
        It.IsAny<string>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<IDictionary<string, object?>>(),
        It.IsAny<IAsyncBasicConsumer>(),
        It.IsAny<CancellationToken>()))
        .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
            (_, _, _, _, _, _, consumer, _) => capturedConsumer = consumer)
        .ReturnsAsync("tag");

    var results = new List<BasicDeliverEventArgs>();
    using var cts = new CancellationTokenSource();

    // Act
    var consumeTask = Task.Run(async () =>
    {
      try
      {
        await foreach (var msg in sut.ConsumeAsync(cts.Token))
        {
          results.Add(msg);
          // Signal completion by canceling after receipt
          await cts.CancelAsync();
        }
      }
      catch (OperationCanceledException)
      {
        // Expected behavior when yielding stops
      }
    });

    // Wait for consumer wiring
    var start = DateTime.UtcNow;
    while (capturedConsumer == null && (DateTime.UtcNow - start).TotalSeconds < 2)
      await Task.Delay(10);

    capturedConsumer.Should().NotBeNull("Consumer should have been registered via BasicConsumeAsync");

    // Simulate RabbitMQ delivery
    var properties = new BasicProperties();
    await capturedConsumer!.HandleBasicDeliverAsync(
        consumerTag: "tag",
        deliveryTag: 1,
        redelivered: false,
        exchange: "ex",
        routingKey: "rk",
        properties: properties,
        body: new ReadOnlyMemory<byte>([123]));

    // Ensure the task finishes without hanging
    await consumeTask.WaitAsync(TimeSpan.FromSeconds(5));

    // Assert
    results.Should().HaveCount(1);
    results[0].DeliveryTag.Should().Be(1);
    results[0].Body.ToArray().Should().Equal([123]);
  }
}