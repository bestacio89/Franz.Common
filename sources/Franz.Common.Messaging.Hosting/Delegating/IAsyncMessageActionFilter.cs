using Franz.Common.DependencyInjection;

namespace Franz.Common.Messaging.Hosting.Delegating;

public interface IAsyncMessageActionFilter : IScopedDependency
{
    Task OnActionExecutionAsync(MessageActionExecutingContext context, MessageActionExecutionDelegate next);
}
