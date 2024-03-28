using Franz.Common.DependencyInjection;

namespace Franz.Common.Messaging.Factories;

public interface IMessageBuilderStrategy : IScopedDependency
{
  bool CanBuild(object value);

  Message Build(object value);
}
