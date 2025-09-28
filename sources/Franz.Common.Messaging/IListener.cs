namespace Franz.Common.Messaging.Hosting;

public interface IListener
{
  event EventHandler<MessageEventArgs> Received;

  /// <summary>
  /// Start listening asynchronously.
  /// </summary>
  Task Listen(CancellationToken stoppingToken = default);

  /// <summary>
  /// Stop listening and clean up resources.
  /// </summary>
  void StopListen();
}
