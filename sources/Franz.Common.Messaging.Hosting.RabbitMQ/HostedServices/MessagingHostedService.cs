using Franz.Common.Errors;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;

public sealed class MessagingHostedService : IHostedService
{
  private readonly IListener _listener;
  private readonly IServiceProvider _serviceProvider;

  public MessagingHostedService(IListener _listener, IServiceProvider _serviceProvider)
  {
    this._listener = _listener;
    this._serviceProvider = _serviceProvider;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    // 🚀 THE FIX: Register the Task-based delegate
    _listener.OnMessageReceivedAsync = HandleMessageAsync;

    // Note: Running in a background task to avoid blocking the Host startup
    _ = Task.Run(() => _listener.Listen(cancellationToken), cancellationToken);

    return Task.CompletedTask;
  }

  private async Task HandleMessageAsync(MessageEventArgs messageEventArgs)
  {
    var message = messageEventArgs.Message;

    // 🛡️ Scope is created and disposed strictly within the Task lifecycle
    using var scope = _serviceProvider.CreateScope();

    var messageContextAccessor = scope.ServiceProvider.GetRequiredService<MessageContextAccessor>();
    messageContextAccessor.Set(new MessageContext(message));

    var executers = scope.ServiceProvider.GetServices<IMessagingStrategyExecuter>();

    // ⚡ .NET 10 Optimization: Avoid .Result; use async discovery
    IMessagingStrategyExecuter? executer = null;
    foreach (var e in executers)
    {
      if (await e.CanExecuteAsync(message))
      {
        executer = e;
        break;
      }
    }

    if (executer == null)
      throw new TechnicalException(Resources.StrategyExecuterNotFoundException);

    // ✅ Non-blocking execution
    await executer.ExecuteAsync(message);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    // Clean up delegate to prevent memory leaks or late-arriving messages
    _listener.OnMessageReceivedAsync = null;
    await _listener.StopListenAsync(cancellationToken);
  }
}