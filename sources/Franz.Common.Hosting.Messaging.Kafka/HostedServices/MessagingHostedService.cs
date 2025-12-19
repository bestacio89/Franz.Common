using Franz.Common.Errors;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

public sealed class MessagingHostedService : BackgroundService
{
  private readonly IListener _listener;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<MessagingHostedService> _logger;

  public MessagingHostedService(
      IListener listener,
      IServiceProvider serviceProvider,
      ILogger<MessagingHostedService> logger)
  {
    _listener = listener;
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _listener.Received += OnMessageReceivedAsync;

    try
    {
      _logger.LogInformation("🚀 MessagingHostedService started");
      await _listener.Listen(stoppingToken);
    }
    catch (OperationCanceledException)
    {
      // Expected on shutdown
    }
    catch (Exception ex)
    {
      _logger.LogCritical(ex, "❌ MessagingHostedService crashed");
      throw;
    }
    finally
    {
      _listener.Received -= OnMessageReceivedAsync;
      _logger.LogInformation("🛑 MessagingHostedService stopped");
    }
  }

  private async void OnMessageReceivedAsync(object? sender, MessageEventArgs args)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();

      var messageContextAccessor =
          scope.ServiceProvider.GetRequiredService<MessageContextAccessor>();

      messageContextAccessor.Set(new MessageContext(args.Message));

      var executers =
          scope.ServiceProvider.GetServices<IMessagingStrategyExecuter>();

      var executer = executers
          .FirstOrDefault(x => x.CanExecuteAsync(args.Message).GetAwaiter().GetResult());

      if (executer is null)
        throw new TechnicalException(Resources.StrategyExecuterNotFoundException);

      await executer.ExecuteAsync(args.Message);
    }
    catch (Exception ex)
    {
      // Do NOT crash the host because of a bad message
      _logger.LogError(ex, "❌ Error handling incoming message");
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _listener.StopListen();
    await base.StopAsync(cancellationToken);
  }
}
