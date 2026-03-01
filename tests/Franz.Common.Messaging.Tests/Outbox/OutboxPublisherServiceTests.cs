using FluentAssertions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Outbox;

public class OutboxPublisherServiceTests
{
  private readonly Mock<IMessageStore> _mockStore = new();
  private readonly Mock<IMessagingSender> _mockSender = new();
  private readonly Mock<ILogger<OutboxPublisherService>> _mockLogger = new();
  private readonly OutboxOptions _options = new() { PollingInterval = TimeSpan.FromMilliseconds(10), MaxRetries = 2 };

  private OutboxPublisherService CreateService() =>
      new(_mockStore.Object, _mockSender.Object, Options.Create(_options), _mockLogger.Object);

  [Fact]
  public async Task ExecuteAsync_WhenMessagesPending_ShouldSendAndMarkAsSent()
  {
    // Arrange
    var cts = new CancellationTokenSource();
    var storedMessage = new StoredMessage { Id = Guid.CreateVersion7(), MessageType = "Test" };

    _mockStore.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage });

    // Act: We run the loop once and then cancel
    var service = CreateService();
    var task = service.StartAsync(cts.Token);

    await Task.Delay(50); // Let one loop iteration run
    await cts.CancelAsync();

    // Assert
    _mockSender.Verify(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    _mockStore.Verify(s => s.MarkAsSentAsync(storedMessage.Id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
  }

  [Fact]
  public async Task ExecuteAsync_WhenSenderFails_ShouldIncrementRetryCount()
  {
    // Arrange
    var cts = new CancellationTokenSource();
    var storedMessage = new StoredMessage { Id = Guid.CreateVersion7(), RetryCount = 0 };

    _mockStore.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage });

    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Network Down"));

    // Act
    var service = CreateService();
    _ = service.StartAsync(cts.Token);

    await Task.Delay(50);
    await cts.CancelAsync();

    // Assert
    _mockStore.Verify(s => s.UpdateRetryAsync(It.Is<StoredMessage>(m => m.RetryCount == 1), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
  }

  [Fact]
  public async Task ExecuteAsync_WhenMaxRetriesExceeded_ShouldMoveToDeadLetter()
  {
    // Arrange
    var cts = new CancellationTokenSource();
    // Start at 1 retry, Max is 2. Next failure should trigger DLQ
    var storedMessage = new StoredMessage { Id = Guid.CreateVersion7(), RetryCount = 1 };

    _mockStore.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage });

    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Permanent Failure"));

    // Act
    var service = CreateService();
    _ = service.StartAsync(cts.Token);

    await Task.Delay(50);
    await cts.CancelAsync();

    // Assert
    _mockStore.Verify(s => s.MoveToDeadLetterAsync(It.IsAny<StoredMessage>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    _mockStore.Verify(s => s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<CancellationToken>()), Times.Never);
  }
}