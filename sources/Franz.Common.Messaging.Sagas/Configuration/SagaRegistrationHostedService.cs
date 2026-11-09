using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Sagas.Configuration;

/// <summary>
/// Validates and registers saga mappings before the host begins serving messages.
/// </summary>
internal sealed class SagaRegistrationHostedService(IServiceProvider services) : IHostedService
{
  public Task StartAsync(CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    services.BuildFranzSagas();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
