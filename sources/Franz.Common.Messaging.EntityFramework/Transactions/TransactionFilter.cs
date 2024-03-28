using Franz.Common.Messaging.Hosting.Delegating;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.EntityFramework.Transactions;
public class TransactionFilter : IAsyncMessageActionFilter
{
    private readonly DbContext dbContext;

    public TransactionFilter(DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task OnActionExecutionAsync(MessageActionExecutingContext context, MessageActionExecutionDelegate next)
    {
        var executedContext = await next.Invoke();

        if (dbContext.Database.CurrentTransaction is not null)
        {
            if (executedContext.Exception == null)
                await dbContext.Database.CurrentTransaction.CommitAsync();
            else
                await dbContext.Database.CurrentTransaction.RollbackAsync();
        }
    }
}
