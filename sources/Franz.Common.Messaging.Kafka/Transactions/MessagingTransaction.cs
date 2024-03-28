using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Modeling;

namespace Franz.Common.Messaging.Kafka.Transactions
{
  public sealed class MessagingTransaction : IMessagingTransaction
  {
    private readonly IProducer<string, string> _producer;

    public MessagingTransaction(IProducer<string, string> producer)
    {
      _producer = producer;
    }

    public void Begin()
    {
      _producer.BeginTransaction();
    }

    public void Complete()
    {
      _producer.CommitTransaction();
    }

    public void Rollback()
    {
      _producer.AbortTransaction();
    }
  }
}
