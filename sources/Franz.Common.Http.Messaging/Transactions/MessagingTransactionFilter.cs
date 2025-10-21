using Franz.Common.Messaging;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Franz.Common.Http.Messaging.Transactions;
public class MessagingTransactionFilter : IAsyncActionFilter
{
  private readonly IMessagingTransaction messagingTransaction;

  public MessagingTransactionFilter(IMessagingTransaction messagingTransaction)
  {
    this.messagingTransaction = messagingTransaction;
  }

  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    var executedContext = await next.Invoke();

    if (executedContext.Exception == null)
      messagingTransaction.Complete();
    else
      messagingTransaction.Rollback();
  }
}
