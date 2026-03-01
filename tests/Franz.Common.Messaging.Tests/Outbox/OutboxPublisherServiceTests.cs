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

    // Setup the store to return the pending message
    _mockStore.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage });

    // Setup the sender to throw, simulating a failure
    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Network Down"));

    // Capture the message passed to UpdateRetryAsync
    StoredMessage updatedMessage = null;
    _mockStore
        .Setup(s => s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<CancellationToken>()))
        .Callback<StoredMessage, CancellationToken>((m, _) => updatedMessage = m)
        .Returns(Task.CompletedTask);

    // Act
    var service = CreateService();
    _ = service.StartAsync(cts.Token);

    // Wait a short moment to allow the async loop to process
    await Task.Delay(50);

    // Cancel the service loop cleanly
    cts.Cancel();

    // Assert
    updatedMessage.Should().NotBeNull("UpdateRetryAsync should have been called at least once");
    updatedMessage.RetryCount.Should().BeGreaterThan(0, "RetryCount should have been incremented after a send failure");

    // Optional: verify that UpdateRetryAsync was actually called at least once
    _mockStore.Verify(s => s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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