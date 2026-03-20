using System;
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Messaging.Kafka.Modeling;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Transactions
{
  public class MessagingTransactionTests
  {
    private readonly Mock<IProducer<string, string>> _producerMock;
    private readonly MessagingTransaction _transaction;

    public MessagingTransactionTests()
    {
      _producerMock = new Mock<IProducer<string, string>>(MockBehavior.Strict);
      _transaction = new MessagingTransaction(_producerMock.Object);
    }

    [Fact]
    public void Begin_CallsProducerBeginTransaction()
    {
      // Arrange
      _producerMock.Setup(p => p.BeginTransaction());

      // Act
      _transaction.Begin();

      // Assert
      _producerMock.Verify(p => p.BeginTransaction(), Times.Once);
    }

    [Fact]
    public void Complete_CallsProducerCommitTransaction()
    {
      // Arrange
      _producerMock.Setup(p => p.CommitTransaction());

      // Act
      _transaction.Complete();

      // Assert
      _producerMock.Verify(p => p.CommitTransaction(), Times.Once);
    }

    [Fact]
    public void Rollback_CallsProducerAbortTransaction()
    {
      // Arrange
      _producerMock.Setup(p => p.AbortTransaction());

      // Act
      _transaction.Rollback();

      // Assert
      _producerMock.Verify(p => p.AbortTransaction(), Times.Once);
    }

    [Fact]
    public void Begin_WhenProducerThrows_PropagatesException()
    {
      // Arrange
      _producerMock.Setup(p => p.BeginTransaction()).Throws(new InvalidOperationException("Kafka error"));

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => _transaction.Begin());
      Assert.Equal("Kafka error", ex.Message);
    }

    [Fact]
    public void Complete_WhenProducerThrows_PropagatesException()
    {
      // Arrange
      _producerMock.Setup(p => p.CommitTransaction()).Throws(new InvalidOperationException("Kafka error"));

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => _transaction.Complete());
      Assert.Equal("Kafka error", ex.Message);
    }

    [Fact]
    public void Rollback_WhenProducerThrows_PropagatesException()
    {
      // Arrange
      _producerMock.Setup(p => p.AbortTransaction()).Throws(new InvalidOperationException("Kafka error"));

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => _transaction.Rollback());
      Assert.Equal("Kafka error", ex.Message);
    }
  }
}