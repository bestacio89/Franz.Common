using Franz.Common.Mediator.OpenTelemetry;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

public static class OpenTelemetryMediatorExtensions
{
  public static IServiceCollection AddMediatorOpenTelemetry(this IServiceCollection services, string sourceName = "Franz.Mediator")
  {
    services.AddSingleton(new ActivitySource(sourceName));
    services.AddScoped(typeof(IPipeline<,>), typeof(OpenTelemetryPipeline<,>));
    return services;
  }
}
