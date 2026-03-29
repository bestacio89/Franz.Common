using System.Threading.Tasks;

namespace Franz.Common.Messaging.Hosting;

public interface IListener
{
  /// <summary>
  /// Async delegate for message processing. 
  /// Ensures the listener can await the entire downstream pipeline.
  /// </summary>
  Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

  /// <summary>
  /// Start listening asynchronously.
  /// </summary>
  Task Listen(CancellationToken stoppingToken = default);

  /// <summary>
  /// Stop listening and clean up resources asynchronously.
  /// </summary>
  Task StopListenAsync(CancellationToken cancellationToken = default);
}
