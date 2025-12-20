using Franz.Common.DependencyInjection;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging;

public interface IMessageBuilder : IScopedDependency
{
  public bool CanBuild(Message message);

  public void Build(Message message);
}
