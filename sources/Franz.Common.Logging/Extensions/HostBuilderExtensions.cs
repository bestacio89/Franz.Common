using Franz.Common.Logging.Tracing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

#if DEBUG
using Elastic.Apm;
using Elastic.Apm.NetCoreAll;
#endif

namespace Franz.Common.Logging.Extensions
{
  public static class HostBuilderExtensions
  {
    /// <summary>
    /// Strict environment-aware logging (hardcoded sinks).
    /// Dev → Console + Debug + File
    /// Prod → Console + JSON + File
    /// </summary>
    public static IHostBuilder UseLog(this IHostBuilder hostBuilder)
    {
      // Early bootstrap console logging
      TraceHelper.LogConsole();

      hostBuilder.UseSerilog((context, services, configuration) =>
      {
        var env = context.HostingEnvironment;

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", env.ApplicationName)
            .Enrich.WithProperty("Environment", env.EnvironmentName);

        if (env.IsDevelopment())
        {
          configuration.MinimumLevel.Debug()
              .WriteTo.Console(
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
              .WriteTo.Debug()
              .WriteTo.File("logs/dev-.log", rollingInterval: RollingInterval.Day);
        }
        else if (env.IsProduction())
        {
          configuration.MinimumLevel.Information()
              .WriteTo.Console(
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
              .WriteTo.File(
                  path: "logs/prod-.json",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30,
                  formatter: new Serilog.Formatting.Json.JsonFormatter())
              .WriteTo.File(
                  path: "logs/prod-.log",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30);
        }
      });

#if DEBUG
            hostBuilder.ConfigureServices((_, __) =>
            {
                Agent.Setup(new AgentComponents());
            });
#endif

      return hostBuilder;
    }

    /// <summary>
    /// Hybrid logging: read sinks from appsettings.json (Development/Production),
    /// only enforce enrichers + app/env fields in code.
    /// </summary>
    public static IHostBuilder UseHybridLog(this IHostBuilder hostBuilder)
    {
      // Early bootstrap console logging
      TraceHelper.LogConsole();

      hostBuilder.UseSerilog((context, services, configuration) =>
      {
        var env = context.HostingEnvironment;

        configuration
            .ReadFrom.Configuration(context.Configuration) // all sinks from JSON
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", env.ApplicationName)
            .Enrich.WithProperty("Environment", env.EnvironmentName);
      });

#if DEBUG
            hostBuilder.ConfigureServices((_, __) =>
            {
                Agent.Setup(new AgentComponents());
            });
#endif

      return hostBuilder;
    }
  }
}
