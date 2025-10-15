using Franz.Common.Logging.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Text;

#if DEBUG
using Elastic.Apm;
using Elastic.Apm.NetCoreAll;
#endif

namespace Franz.Common.Logging.Extensions
{
  public static class HostBuilderExtensions
  {
    static HostBuilderExtensions()
    {
      // ⚙️ Global UTF-8 enforcement at process level
      Console.OutputEncoding = Encoding.UTF8;
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Strict environment-aware logging with enforced UTF-8 support.
    /// Dev → Console + Debug + File
    /// Prod → Console + JSON + File
    /// </summary>
    public static IHostBuilder UseLog(this IHostBuilder hostBuilder)
    {
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

        // Ensure all text sinks write UTF-8
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        if (env.IsDevelopment())
        {
          configuration.MinimumLevel.Debug()
              .WriteTo.Console(
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                  )
              .WriteTo.Debug()
              .WriteTo.File(
                  path: "logs/dev-.log",
                  rollingInterval: RollingInterval.Day,
                  encoding: utf8);
        }
        else if (env.IsProduction())
        {
          configuration.MinimumLevel.Information()
              .WriteTo.Console(
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                  )
              .WriteTo.File(
                  path: "logs/prod-.json",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30,
                  encoding: utf8,
                  formatter: new Serilog.Formatting.Json.JsonFormatter())
              .WriteTo.File(
                  path: "logs/prod-.log",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30,
                  encoding: utf8);
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
    /// Hybrid mode (config-driven sinks) with UTF-8 enforced.
    /// </summary>
    public static IHostBuilder UseHybridLog(this IHostBuilder hostBuilder)
    {
      TraceHelper.LogConsole();
      Console.OutputEncoding = Encoding.UTF8;

      hostBuilder.UseSerilog((context, services, configuration) =>
      {
        var env = context.HostingEnvironment;

        configuration
            .ReadFrom.Configuration(context.Configuration)
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
