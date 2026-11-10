#nullable enable

using Franz.Common.Messaging.Sagas.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Sagas.Configuration;

/// <summary>
/// Performs bounded, periodic deletion of expired durable saga audit records.
/// </summary>
internal sealed class SagaAuditRetentionHostedService : BackgroundService, IHostedLifecycleService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly FranzSagaOptions _options;
  private readonly ILogger<SagaAuditRetentionHostedService> _logger;

  public SagaAuditRetentionHostedService(
      IServiceScopeFactory scopeFactory,
      FranzSagaOptions options,
      ILogger<SagaAuditRetentionHostedService> logger)
  {
    _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    _options = options ?? throw new ArgumentNullException(nameof(options));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task StartingAsync(CancellationToken cancellationToken)
  {
    await using var scope = _scopeFactory.CreateAsyncScope();
    var store = scope.ServiceProvider.GetRequiredService<ISagaAuditStore>();
    await store.InitializeAsync(cancellationToken).ConfigureAwait(false);
  }

  public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(_options.AuditRetentionCleanupInterval);
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<ISagaAuditStore>();
        var cutoff = DateTimeOffset.UtcNow - _options.AuditRetentionPeriod!.Value;
        var deleted = await store.DeleteExpiredAsync(
            cutoff,
            _options.AuditRetentionBatchSize,
            stoppingToken).ConfigureAwait(false);

        if (deleted > 0)
          _logger.LogInformation(
              "Removed {DeletedCount} expired saga audit records older than {Cutoff}.",
              deleted,
              cutoff);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception exception)
      {
        _logger.LogError(exception, "Saga audit retention cleanup failed.");
      }

      if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        break;
    }
  }
}
