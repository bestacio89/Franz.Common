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
    // ✅ Register the Task-based delegate
    _listener.OnMessageReceivedAsync = HandleMessageInternalAsync;

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
      // ✅ Clean up delegate reference
      _listener.OnMessageReceivedAsync = null;
      _logger.LogInformation("🛑 MessagingHostedService stopped");
    }
  }

  // ✅ Changed from 'async void' to 'async Task' to allow the listener to await completion
  private async Task HandleMessageInternalAsync(MessageEventArgs args)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();

      var messageContextAccessor =
          scope.ServiceProvider.GetRequiredService<MessageContextAccessor>();

      messageContextAccessor.Set(new MessageContext(args.Message));

      var executers =
          scope.ServiceProvider.GetServices<IMessagingStrategyExecuter>();

      // ✅ Senior Architect Rule: Avoid .GetAwaiter().GetResult() in async paths.
      // Use an asynchronous search for the strategy executer.
      IMessagingStrategyExecuter? executer = null;
      foreach (var e in executers)
      {
        if (await e.CanExecuteAsync(args.Message))
        {
          executer = e;
          break;
        }
      }

      if (executer is null)
        throw new TechnicalException(Resources.StrategyExecuterNotFoundException);

      await executer.ExecuteAsync(args.Message);
    }
    catch (Exception ex)
    {
      // Do NOT crash the host because of a bad message, but log it properly.
      _logger.LogError(ex, "❌ Error handling incoming message {MessageId}", args.Message.Id);

      // Note: If you want Kafka to retry, you might re-throw here so the Listener 
      // doesn't commit the offset.
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    await _listener.StopListenAsync(cancellationToken);
    await base.StopAsync(cancellationToken);
  }
}