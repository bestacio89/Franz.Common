using Franz.Common.DependencyInjection;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Hosting.Executing;

public interface IMessagingStrategyExecuter : IScopedDependency
{
  Task<bool> CanExecuteAsync(Message message);

  Task ExecuteAsync(Message message);
}
