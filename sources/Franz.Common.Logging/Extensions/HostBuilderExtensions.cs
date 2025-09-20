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
    public static IHostBuilder UseLog(this IHostBuilder hostBuilder)
    {
      // Early bootstrap console logging (TraceHelper)
      TraceHelper.LogConsole();

      hostBuilder.UseSerilog((context, services, _) =>
      {
        var env = context.HostingEnvironment;

        var config = new LoggerConfiguration()
          .ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithProperty("Application", env.ApplicationName)
          .Enrich.WithProperty("Environment", env.EnvironmentName);

        if (env.IsDevelopment())
        {
          // DEV: verbose, human-friendly
          config.MinimumLevel.Debug()
                .WriteTo.Console(
                  outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.Debug(
                  outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                  "logs/dev-.log",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 7,
                  shared: true,
                  flushToDiskInterval: TimeSpan.FromSeconds(1),
                  outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                );
        }
        else if (env.IsProduction())
        {
          // PROD: clean, structured, longer retention
          config.MinimumLevel.Information()
                .WriteTo.Console(
                  outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                  path: "logs/prod-.json",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30,
                  shared: true,
                  flushToDiskInterval: TimeSpan.FromSeconds(5),
                  formatter: new Serilog.Formatting.Json.JsonFormatter() // structured logs for ingestion
                )
                .WriteTo.File(
                  path: "logs/prod-.log",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30,
                  shared: true,
                  flushToDiskInterval: TimeSpan.FromSeconds(5),
                  outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                );
        }

        Log.Logger = config.CreateLogger();
      });

#if DEBUG
      // Enable Elastic APM diagnostics in debug builds
      hostBuilder.ConfigureServices((_, __) =>
      {
        Agent.Setup(new AgentComponents());
      });
#endif

      return hostBuilder;
    }
  }
}
