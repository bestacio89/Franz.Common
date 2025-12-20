using Franz.Common.DependencyInjection;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Factories;

public interface IMessageBuilderStrategy : IScopedDependency
{
  bool CanBuild(object value);

  Message Build(object value);
}
