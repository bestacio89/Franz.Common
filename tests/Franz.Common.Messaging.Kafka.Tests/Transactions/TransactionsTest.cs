#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Transactions;
using Moq;
using Xunit;
using FluentAssertions;

namespace Franz.Common.Messaging.Kafka.Tests.Transactions;

[Collection("Kafka")]
public class MessagingTransactionTests
{
  private readonly Mock<IProducer<string, byte[]>> _producerMock;
  private readonly MessagingTransaction _sut;

  public MessagingTransactionTests()
  {
    _producerMock = new Mock<IProducer<string, byte[]>>();
    _sut = new MessagingTransaction(_producerMock.Object);
  }

  [Fact]
  public async Task BeginAsync_ShouldInvokeBeginOnProducer()
  {
    // Act
    await _sut.BeginAsync();

    // Assert
    _producerMock.Verify(x => x.BeginTransaction(), Times.Once);
  }

  [Fact]
  public async Task CompleteAsync_ShouldCommitTransaction_WhenActive()
  {
    // Arrange
    await _sut.BeginAsync();

    // Act
    await _sut.CompleteAsync();

    // Assert
    _producerMock.Verify(x => x.CommitTransaction(), Times.Once);
  }

  [Fact]
  public async Task RollbackAsync_ShouldAbortTransaction_WhenActive()
  {
    // Arrange
    await _sut.BeginAsync();

    // Act
    await _sut.RollbackAsync();

    // Assert
    _producerMock.Verify(x => x.AbortTransaction(), Times.Once);
  }

  [Fact]
  public async Task DisposeAsync_ShouldAbortActiveTransaction_ToPreventZombieState()
  {
    // Arrange
    await _sut.BeginAsync();

    // Act
    await _sut.DisposeAsync();

    // Assert: Senior Architect Rule - Automatic rollback on disposal
    _producerMock.Verify(x => x.AbortTransaction(), Times.Once);
  }

  [Fact]
  public async Task CompleteAsync_ShouldNotInvokeProducer_WhenNotStarted()
  {
    // Act
    await _sut.CompleteAsync();

    // Assert
    _producerMock.Verify(x => x.CommitTransaction(), Times.Never);
  }
}