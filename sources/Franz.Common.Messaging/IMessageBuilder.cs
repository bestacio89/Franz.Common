using Franz.Common.DependencyInjection;

namespace Franz.Common.Messaging;

public interface IMessageBuilder : IScopedDependency
{
  public bool CanBuild(Message message);

  public void Build(Message message);
}
