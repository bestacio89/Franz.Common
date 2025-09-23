

using Franz.Common.Mediator.Messages;

namespace Franz.Common.Messaging;

public interface IMessagingSender
{
   Task SendAsync<TCommandBaseRequest>(TCommandBaseRequest command, CancellationToken cancellationToken = default)
        where TCommandBaseRequest : ICommand;
  
}
