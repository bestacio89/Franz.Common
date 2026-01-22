using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.OpenTelemetry.Pipelines;
using Franz.Common.Mediator.OpenTelemetry;
using Franz.Common.Mediator.Validation.Events;
using Franz.Common.Mediator.OpenTelemetry.Core;

public static class FranzTelemetryServiceCollectionExtensions
{
  public static IServiceCollection AddFranzTelemetry(
      this IServiceCollection services,
      IHostEnvironment env,
      IConfiguration config)
  {
    // 1️⃣ Load options from configuration
    var options = config
        .GetSection("Franz:Telemetry")
        .Get<FranzTelemetryOptions>()
        ?? new FranzTelemetryOptions();

    var samplingRatio = FranzTelemetryDefaults.ResolveSamplingRatio(env, options);
    var sourceName = options.SourceName ?? "Franz.Application";

    // 2️⃣ Register ActivitySource for manual instrumentation
    services.AddSingleton(new ActivitySource(sourceName));

    // 3️⃣ Ensure OTLP endpoint is valid in production
    var otlpEndpoint = options.OtlpEndpoint;
    if (string.IsNullOrWhiteSpace(otlpEndpoint))
    {
      if (!env.IsDevelopment())
      {
        throw new InvalidOperationException(
            "OTLP endpoint is required in production. " +
            "Please configure Franz:Telemetry:OtlpEndpoint.");
      }

      // Dev fallback
      otlpEndpoint = "http://localhost:4317";
    }

    // 4️⃣ Configure OpenTelemetry tracing
    services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName: options.ServiceName ?? env.ApplicationName,
                        serviceVersion: options.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
              ["env"] = env.EnvironmentName
            }))
        .WithTracing(tracing =>
        {
          // Sampler & ActivitySource
          tracing.AddSource(sourceName)
                     .SetSampler(new ParentBasedSampler(
                         new TraceIdRatioBasedSampler(samplingRatio)));

          // Automatic instrumentation
          tracing.AddAspNetCoreInstrumentation();
          tracing.AddHttpClientInstrumentation();

          // Exporters
          if (options.ExportToConsole)
            tracing.AddConsoleExporter();

          tracing.AddOtlpExporter(otlpOptions =>
          {
            otlpOptions.Endpoint = new Uri(otlpEndpoint);
          });
        });

    // 5️⃣ Register Mediator pipelines
    services.AddScoped(typeof(IPipeline<,>), typeof(OpenTelemetryPipeline<,>));
    services.AddScoped(typeof(IEventPipeline<>), typeof(EventTracingPipeline<>));

    return services;
  }
}
