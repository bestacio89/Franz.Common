using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Testing;
using Moq;
using NUnit.Framework;

namespace Franz.Common.Messaging.Kafka.Tests.Transactions
{
  [TestFixture]
  public class MessagingTransactionTests:UnitTest
  {
    private readonly Mock<IProducer<string, string>> _producerMock;
    private readonly MessagingTransaction _transaction;

    public MessagingTransactionTests()
    {
      _producerMock = new Mock<IProducer<string, string>>();
      _transaction = new MessagingTransaction(_producerMock.Object);
    }

    [Test]
    public void Begin_ShouldBeginTransaction()
    {
      _transaction.Begin();
      _producerMock.Verify(x => x.BeginTransaction(), Times.Once);
    }

    [Test]
    public void Complete_ShouldCommitTransaction()
    {
      _transaction.Complete();
      _producerMock.Verify(x => x.CommitTransaction(), Times.Once);
    }

    [Test]
    public void Rollback_ShouldAbortTransaction()
    {
      _transaction.Rollback();
      _producerMock.Verify(x => x.AbortTransaction(), Times.Once);
    }
  }
}
