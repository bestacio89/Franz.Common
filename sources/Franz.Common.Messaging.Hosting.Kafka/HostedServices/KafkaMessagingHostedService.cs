using Franz.Common.Errors;
using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

public sealed class KafkaMessagingHostedService : BackgroundService
{
  private readonly IListener _listener;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<KafkaMessagingHostedService> _logger;

  public KafkaMessagingHostedService(
      IListener listener,
      IServiceProvider serviceProvider,
      ILogger<KafkaMessagingHostedService> logger)
  {
    _listener = listener;
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _listener.OnMessageReceivedAsync = HandleMessageInternalAsync;

    try
    {
      _logger.LogInformation("Messaging hosted service started.");
      await _listener.Listen(stoppingToken);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
      // Expected during graceful shutdown.
    }
    catch (Exception ex)
    {
      _logger.LogCritical(ex, "Messaging hosted service stopped after an unrecoverable listener failure.");
      throw;
    }
    finally
    {
      _listener.OnMessageReceivedAsync = null;
      _logger.LogInformation("Messaging hosted service stopped.");
    }
  }

  private async Task HandleMessageInternalAsync(MessageEventArgs args)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();

      var messageContextAccessor =
          scope.ServiceProvider.GetRequiredService<MessageContextAccessor>();

      messageContextAccessor.Set(new MessageContext(args.Message));

    var tenantInitializer = scope.ServiceProvider.GetService<ITenantMessageContextInitializer>();
    await using var tenantExecutionScope = tenantInitializer is null
        ? null
        : await tenantInitializer.BeginAsync(args.Message);

      var executers =
          scope.ServiceProvider.GetServices<IMessagingStrategyExecuter>();

      IMessagingStrategyExecuter? executer = null;
      foreach (var candidate in executers)
      {
        if (await candidate.CanExecuteAsync(args.Message))
        {
          executer = candidate;
          break;
        }
      }

      if (executer is null)
        throw new TechnicalException(Resources.StrategyExecuterNotFoundException);

      await executer.ExecuteAsync(args.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Message handler failed for message {MessageId}. The failure is propagated so the transport cannot acknowledge it as successful.", args.Message.Id);
      throw;
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    await _listener.StopListenAsync(cancellationToken);
    await base.StopAsync(cancellationToken);
  }
}
