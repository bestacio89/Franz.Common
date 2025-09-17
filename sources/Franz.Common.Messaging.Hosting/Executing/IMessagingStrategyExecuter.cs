using Franz.Common.DependencyInjection;

namespace Franz.Common.Messaging.Hosting.Executing;

public interface IMessagingStrategyExecuter : IScopedDependency
{
  Task<bool> CanExecuteAsync(Message message);

  Task ExecuteAsync(Message message);
}
