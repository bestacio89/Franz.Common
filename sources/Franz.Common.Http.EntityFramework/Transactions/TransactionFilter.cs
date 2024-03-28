using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Http.EntityFramework.Transactions;
public class TransactionFilter : IAsyncActionFilter
{
    private readonly DbContext dbContext;

    public TransactionFilter(DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
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
