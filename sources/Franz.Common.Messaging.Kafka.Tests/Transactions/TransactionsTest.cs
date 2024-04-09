using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Testing;
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
      _producerMock = new Mock<IProducer<string, string>>();
      _transaction = new MessagingTransaction(_producerMock.Object);
    }

    [Fact]
    public void Begin_ShouldBeginTransaction()
    {
      _transaction.Begin();

      _producerMock.Verify(x => x.BeginTransaction(), Times.Once);
    }

    [Fact]
    public void Complete_ShouldCommitTransaction()
    {
      _transaction.Complete();

      _producerMock.Verify(x => x.CommitTransaction(), Times.Once);
    }

    [Fact]
    public void Rollback_ShouldAbortTransaction()
    {
      _transaction.Rollback();

      _producerMock.Verify(x => x.AbortTransaction(), Times.Once);
    }
  }
}
