

using Franz.Common.Mediator.Messages;

namespace Franz.Common.Messaging;

public interface IMessagingSender
{
    void Send<TCommandBaseRequest>(TCommandBaseRequest command)
      where TCommandBaseRequest : ICommand;
}
