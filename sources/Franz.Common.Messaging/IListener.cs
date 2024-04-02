namespace Franz.Common.Messaging.Hosting;

public interface IListener
{
    event EventHandler<MessageEventArgs> Received;

    void Listen();

    void StopListen();
}
