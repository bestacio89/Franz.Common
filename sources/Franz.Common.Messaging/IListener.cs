namespace Franz.Common.Messaging.Hosting;

public interface IListener
{
  event EventHandler<MessageEventArgs> Received;

  Task ListenAsync(); 

  void StopListen();
}