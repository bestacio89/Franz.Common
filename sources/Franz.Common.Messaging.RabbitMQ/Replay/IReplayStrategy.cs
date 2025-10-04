using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;
public interface IReplayStrategy
{
  void Replay(BasicDeliverEventArgs basicDeliverEventArgs, Exception ex);
}
