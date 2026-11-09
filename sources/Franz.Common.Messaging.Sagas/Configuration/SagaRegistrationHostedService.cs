using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Sagas.Configuration;

/// <summary>
/// Finalizes and validates saga registrations in the host lifecycle phase that
/// runs before any hosted service starts consuming messages.
/// </summary>
internal sealed class SagaRegistrationHostedService(IServiceProvider services) : IHostedLifecycleService
{
  public Task StartingAsync(CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    services.BuildFranzSagas();
    return Task.CompletedTask;
  }

  public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
