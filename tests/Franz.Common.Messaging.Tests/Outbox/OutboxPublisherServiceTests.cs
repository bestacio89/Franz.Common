using FluentAssertions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Exceptions;
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
  private readonly OutboxOptions _options = new()
  {
    Enabled = true,
    DeadLetterEnabled = true,
    PollingInterval = TimeSpan.FromMilliseconds(10),
    MaxRetries = 2
  };

  private OutboxPublisherService CreateService() =>
      new(_mockStore.Object, _mockSender.Object, Options.Create(_options), _mockLogger.Object);

  [Fact]
  public async Task ProcessOutboxOnce_ShouldSendAndMarkAsSent()
  {
    // Arrange
    var storedMessage = new StoredMessage
    {
      Id = Guid.CreateVersion7(),
      MessageType = "Test"
    };

    _mockStore.SetupSequence(s => s.ClaimPendingAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage })
        .ReturnsAsync(Array.Empty<StoredMessage>());

    var service = CreateService();

    // Act
    await service.ProcessOutboxOnceAsync(CancellationToken.None);

    // Assert
    _mockStore.Verify(s =>
        s.ClaimPendingAsync(It.IsAny<Guid>(), _options.LeaseDuration, 1, It.IsAny<CancellationToken>()),
        Times.Exactly(2));

    _mockSender.Verify(s =>
        s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
        Times.Once);

    _mockStore.Verify(s =>
        s.MarkAsSentAsync(storedMessage.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task ProcessOutboxOnce_WhenSenderReportsPermanentFailure_ShouldDeadLetterEvenWhenTransientDeadLetteringIsDisabled()
  {
    var options = Options.Create(new OutboxOptions
    {
      Enabled = true,
      DeadLetterEnabled = false,
      MaxRetries = 10,
      BatchSize = 10
    });
    var storedMessage = new StoredMessage { Id = Guid.CreateVersion7(), RetryCount = 0 };
    _mockStore.SetupSequence(s => s.ClaimPendingAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage })
        .ReturnsAsync(Array.Empty<StoredMessage>());
    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new PermanentMessagingException("The configured ordering key is missing."));

    var service = new OutboxPublisherService(_mockStore.Object, _mockSender.Object, options, _mockLogger.Object);
    await service.ProcessOutboxOnceAsync(CancellationToken.None);

    _mockStore.Verify(s => s.MoveToDeadLetterAsync(
        It.Is<StoredMessage>(message => message.Id == storedMessage.Id),
        It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockStore.Verify(s => s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task ProcessOutboxOnce_WhenSenderFails_ShouldIncrementRetryCount()
  {
    // Arrange
    var storedMessage = new StoredMessage
    {
      Id = Guid.CreateVersion7(),
      RetryCount = 0
    };

    _mockStore.SetupSequence(s => s.ClaimPendingAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage })
        .ReturnsAsync(Array.Empty<StoredMessage>());

    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Network Down"));

    StoredMessage? captured = null;

    _mockStore.Setup(s => s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .Callback<StoredMessage, Guid, CancellationToken>((m, _, _) => captured = m)
        .Returns(Task.CompletedTask);

    var service = CreateService();

    // Act
    await service.ProcessOutboxOnceAsync(CancellationToken.None);

    // Assert
    captured.Should().NotBeNull();
    captured!.RetryCount.Should().Be(1);

    _mockStore.Verify(s =>
        s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task ProcessOutboxOnce_WhenMaxRetriesExceeded_ShouldMoveToDeadLetter()
  {
    // Arrange
    var storedMessage = new StoredMessage
    {
      Id = Guid.CreateVersion7(),
      RetryCount = 2 // already at limit (MaxRetries = 2)
    };

    _mockStore.SetupSequence(s => s.ClaimPendingAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage })
        .ReturnsAsync(Array.Empty<StoredMessage>());

    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Permanent Failure"));

    var service = CreateService();

    // Act
    await service.ProcessOutboxOnceAsync(CancellationToken.None);

    // Assert
    _mockStore.Verify(s =>
        s.MoveToDeadLetterAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
        Times.Once);

    _mockStore.Verify(s =>
        s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
        Times.Never);
  }
  [Fact]
  public async Task ProcessOutboxOnce_WhenDeadLetteringIsDisabled_ShouldKeepExhaustedMessagePending()
  {
    var options = Options.Create(new OutboxOptions
    {
      Enabled = true,
      DeadLetterEnabled = false,
      MaxRetries = 2,
      BatchSize = 10
    });
    var storedMessage = new StoredMessage
    {
      Id = Guid.CreateVersion7(),
      RetryCount = 2
    };

    _mockStore.SetupSequence(s => s.ClaimPendingAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage })
        .ReturnsAsync(Array.Empty<StoredMessage>());
    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Permanent Failure"));

    var service = new OutboxPublisherService(
        _mockStore.Object,
        _mockSender.Object,
        options,
        _mockLogger.Object);

    await service.ProcessOutboxOnceAsync(CancellationToken.None);

    _mockStore.Verify(s => s.UpdateRetryAsync(
        It.Is<StoredMessage>(message => message.Id == storedMessage.Id && message.RetryCount == 2),
        It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockStore.Verify(s => s.MoveToDeadLetterAsync(
        It.IsAny<StoredMessage>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task ProcessOutboxOnce_WhenSenderFails_ShouldPersistExponentialRetryEligibility()
  {
    var storedMessage = new StoredMessage { Id = Guid.CreateVersion7() };
    _options.InitialRetryDelay = TimeSpan.FromSeconds(2);
    _options.MaxRetryDelay = TimeSpan.FromSeconds(10);
    _mockStore.SetupSequence(s => s.ClaimPendingAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage })
        .ReturnsAsync(Array.Empty<StoredMessage>());
    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Transient failure"));

    StoredMessage? persisted = null;
    _mockStore.Setup(s => s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .Callback<StoredMessage, Guid, CancellationToken>((message, _, _) => persisted = message)
        .Returns(Task.CompletedTask);

    await CreateService().ProcessOutboxOnceAsync(CancellationToken.None);

    persisted.Should().NotBeNull();
    persisted!.RetryCount.Should().Be(1);
    persisted.NextAttemptAt.Should().NotBeNull();
    persisted.NextAttemptAt!.Value.Should().BeOnOrAfter(persisted.LastTriedOn!.Value.AddSeconds(2));
    persisted.NextAttemptAt.Value.Should().BeOnOrBefore(persisted.LastTriedOn.Value.AddSeconds(2.1));
  }

  [Fact]
  public async Task ProcessOutboxOnce_WhenHostCancellationOccursDuringSend_ShouldNotRecordDeliveryFailure()
  {
    var storedMessage = new StoredMessage { Id = Guid.CreateVersion7() };
    using var cancellation = new CancellationTokenSource();

    _mockStore.SetupSequence(s => s.ClaimPendingAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<StoredMessage> { storedMessage })
        .ReturnsAsync(Array.Empty<StoredMessage>());

    _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        .Returns<Message, CancellationToken>((_, _) =>
        {
          cancellation.Cancel();
          return ValueTask.FromCanceled(cancellation.Token);
        });

    var service = CreateService();

    await FluentActions.Invoking(() => service.ProcessOutboxOnceAsync(cancellation.Token))
        .Should().ThrowAsync<OperationCanceledException>();

    _mockStore.Verify(s => s.UpdateRetryAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    _mockStore.Verify(s => s.MoveToDeadLetterAsync(It.IsAny<StoredMessage>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    _mockStore.Verify(s => s.MarkAsSentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
  }

}
