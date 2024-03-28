using Franz.Common.Business.Commands;

namespace Franz.Common.Messaging;

public interface IMessagingSender
{
    void Send<TCommandBaseRequest>(TCommandBaseRequest command)
      where TCommandBaseRequest : ICommandBaseRequest;
}
