#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Transactions;
using Moq;
using Xunit;

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
    await _sut.BeginAsync();

    _producerMock.Verify(x => x.BeginTransaction(), Times.Once);
  }

  [Fact]
  public async Task CompleteAsync_ShouldCommit_WhenTransactionStarted()
  {
    await _sut.BeginAsync();
    await _sut.CompleteAsync();

    _producerMock.Verify(x => x.CommitTransaction(), Times.Once);
  }

  [Fact]
  public async Task RollbackAsync_ShouldAbort_WhenTransactionStarted()
  {
    await _sut.BeginAsync();
    await _sut.RollbackAsync();

    _producerMock.Verify(x => x.AbortTransaction(), Times.Once);
  }

  [Fact]
  public async Task CompleteAsync_ShouldBeNoOp_WhenNotStarted()
  {
    await _sut.CompleteAsync();

    _producerMock.Verify(x => x.CommitTransaction(), Times.Never);
    _producerMock.Verify(x => x.BeginTransaction(), Times.Never);
  }

  [Fact]
  public async Task RollbackAsync_ShouldBeNoOp_WhenNotStarted()
  {
    await _sut.RollbackAsync();

    _producerMock.Verify(x => x.AbortTransaction(), Times.Never);
  }

  [Fact]
  public async Task DisposeAsync_ShouldAbortOnlyIfTransactionActive()
  {
    await _sut.BeginAsync();

    await _sut.DisposeAsync();

    _producerMock.Verify(x => x.AbortTransaction(), Times.Once);
  }

  [Fact]
  public async Task DisposeAsync_ShouldBeSafe_WhenNoTransactionStarted()
  {
    await _sut.DisposeAsync();

    _producerMock.Verify(x => x.AbortTransaction(), Times.Never);
  }
}