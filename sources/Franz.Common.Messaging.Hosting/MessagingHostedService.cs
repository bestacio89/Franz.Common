using Franz.Common.Errors;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Hosting;

public sealed class MessagingHostedService : IHostedService
{
  private readonly IListener _listener;
  private readonly IServiceProvider _serviceProvider;

  public MessagingHostedService(IListener listener, IServiceProvider serviceProvider)
  {
    _listener = listener;
    _serviceProvider = serviceProvider;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    // ✅ FIX: Register the Task-based delegate instead of the synchronous event
    _listener.OnMessageReceivedAsync = HandleMessageAsync;

    // We await the Listen call. Most IListener implementations (Kafka/Rabbit) 
    // will internally start a long-running loop or task.
    await _listener.Listen(cancellationToken);
  }

  /// <summary>
  /// Processes the incoming message within a dedicated DI scope.
  /// This method is now fully async, allowing the Listener to await its completion.
  /// </summary>
  private async Task HandleMessageAsync(MessageEventArgs messageEventArgs)
  {
    var message = messageEventArgs.Message;

    // 🛡️ Senior Architect Note: The scope is strictly tied to this Task.
    // Because the Listener 'awaits' this Task, the scope is guaranteed 
    // to be disposed ONLY after the strategy execution is finished.
    using var scope = _serviceProvider.CreateScope();

    var messageContextAccessor = scope.ServiceProvider.GetRequiredService<MessageContextAccessor>();
    messageContextAccessor.Set(new MessageContext(message));

    var strategyExecuters = scope.ServiceProvider.GetServices<IMessagingStrategyExecuter>();

    // ⚡ .NET 10 Standards: Use native async iteration to find the correct executer
    IMessagingStrategyExecuter? selectedExecuter = null;
    foreach (var executer in strategyExecuters)
    {
      if (await executer.CanExecuteAsync(message))
      {
        selectedExecuter = executer;
        break;
      }
    }

    if (selectedExecuter == null)
    {
      throw new TechnicalException(Resources.StrategyExecuterNotFoundException);
    }

    // ✅ Non-blocking execution of the message strategy
    await selectedExecuter.ExecuteAsync(message);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    // Clean up the delegate reference to prevent calls during shutdown
    _listener.OnMessageReceivedAsync = null;

    await _listener.StopListenAsync(cancellationToken);
  }
}