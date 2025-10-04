using Franz.Common.Messaging.RabbitMQ.Modeling;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Transactions;

public sealed class MessagingTransaction : IMessagingTransaction
{
  private readonly IModelProvider modelProvider;

  public MessagingTransaction(IModelProvider modelProvider)
  {
    this.modelProvider = modelProvider;
  }

  private IModel Current => modelProvider.Current;

  public void Begin()
  {
    if (!modelProvider.Current.HasTransaction())
      Current.TxSelect();
  }

  public void Complete()
  {
    if (modelProvider.Current.HasTransaction())
      Current.TxCommit();
  }

  public void Rollback()
  {
    if (modelProvider.Current.HasTransaction())
      Current.TxRollback();
  }
}
