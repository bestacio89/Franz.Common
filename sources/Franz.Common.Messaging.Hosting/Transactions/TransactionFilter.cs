using Franz.Common.Messaging.Hosting.Delegating;

namespace Franz.Common.Messaging.Hosting.Transactions;
public class TransactionFilter : IAsyncMessageActionFilter
{
  private readonly IMessagingTransaction messagingTransaction;

  public TransactionFilter(IMessagingTransaction messagingTransaction)
  {
    this.messagingTransaction = messagingTransaction;
  }

  public async Task OnActionExecutionAsync(MessageActionExecutingContext context, MessageActionExecutionDelegate next)
  {
    var executedContext = await next.Invoke();

    if (executedContext.Exception == null)
      await messagingTransaction.CompleteAsync();
    else
      await messagingTransaction.RollbackAsync();
  }
}
