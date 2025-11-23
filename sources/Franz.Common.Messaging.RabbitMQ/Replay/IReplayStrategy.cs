using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public interface IReplayStrategy
{
  Task ReplayAsync(BasicDeliverEventArgs args, Exception ex);
}
