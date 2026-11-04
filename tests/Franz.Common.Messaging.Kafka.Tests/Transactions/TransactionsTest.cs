#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Transactions;
using Moq;
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
  public async Task CompleteAsync_ShouldDisposeIsolatedProducerAndClearTransactionContext()
  {
    var producer = new Mock<IProducer<string, byte[]>>();
    var factory = new Mock<IKafkaTransactionProducerFactory>();
    factory.Setup(x => x.Create()).Returns(producer.Object);
    var context = new KafkaTransactionContext();
    var sut = new MessagingTransaction(factory.Object, context);

    await sut.BeginAsync();
    await sut.CompleteAsync();

    factory.Verify(x => x.Create(), Times.Once);
    producer.Verify(x => x.BeginTransaction(), Times.Once);
    producer.Verify(x => x.CommitTransaction(), Times.Once);
    producer.Verify(x => x.Dispose(), Times.Once);

    // A second transaction in the same scope must acquire a fresh producer.
    factory.Setup(x => x.Create()).Returns(new Mock<IProducer<string, byte[]>>().Object);
    await sut.BeginAsync();
    await sut.RollbackAsync();
    factory.Verify(x => x.Create(), Times.Exactly(2));
  }

  [Fact]
  public async Task BeginAsync_ShouldRejectConcurrentTransactionInSameContext()
  {
    var producer = new Mock<IProducer<string, byte[]>>();
    var factory = new Mock<IKafkaTransactionProducerFactory>();
    factory.Setup(x => x.Create()).Returns(producer.Object);
    var sut = new MessagingTransaction(factory.Object, new KafkaTransactionContext());

    await sut.BeginAsync();
    Func<Task> act = async () => await sut.BeginAsync();

    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*already active*");
    await sut.RollbackAsync();
  }

  [Fact]
  public async Task BeginAsync_ShouldRejectCallsAfterDisposal()
  {
    await _sut.DisposeAsync();
    Func<Task> act = async () => await _sut.BeginAsync();
    await act.Should().ThrowAsync<ObjectDisposedException>();
  }

  [Fact]
  public async Task CompleteAsync_ShouldRejectCallsAfterDisposal()
  {
    await _sut.DisposeAsync();
    Func<Task> act = async () => await _sut.CompleteAsync();
    await act.Should().ThrowAsync<ObjectDisposedException>();
  }

  [Fact]
  public async Task RollbackAsync_ShouldRejectCallsAfterDisposal()
  {
    await _sut.DisposeAsync();
    Func<Task> act = async () => await _sut.RollbackAsync();
    await act.Should().ThrowAsync<ObjectDisposedException>();
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