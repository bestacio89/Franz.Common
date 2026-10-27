using Franz.Common.Errors;
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
      _logger.LogInformation("MessagingHostedService started");
      await _listener.Listen(stoppingToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
      // Expected during graceful shutdown.
    }
    catch (Exception ex)
    {
      _logger.LogCritical(ex, "MessagingHostedService crashed; the failed record remains unacknowledged");
      throw;
    }
    finally
    {
      _listener.OnMessageReceivedAsync = null;
      _logger.LogInformation("MessagingHostedService stopped");
    }
  }

  private async Task HandleMessageInternalAsync(MessageEventArgs args)
  {
    using var scope = _serviceProvider.CreateScope();

    var messageContextAccessor =
      scope.ServiceProvider.GetRequiredService<MessageContextAccessor>();

    messageContextAccessor.Set(new MessageContext(args.Message));

    var executers =
      scope.ServiceProvider.GetServices<IMessagingStrategyExecuter>();

    IMessagingStrategyExecuter? executer = null;
    foreach (var candidate in executers)
    {
      if (await candidate.CanExecuteAsync(args.Message).ConfigureAwait(false))
      {
        executer = candidate;
        break;
      }
    }

    if (executer is null)
      throw new TechnicalException(Resources.StrategyExecuterNotFoundException);

    await executer.ExecuteAsync(args.Message).ConfigureAwait(false);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    await _listener.StopListenAsync(cancellationToken).ConfigureAwait(false);
    await base.StopAsync(cancellationToken).ConfigureAwait(false);
  }
}