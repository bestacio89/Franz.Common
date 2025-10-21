using Franz.Common.Logging.Tracing;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Filters;
using Serilog.Events;
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
      Console.OutputEncoding = Encoding.UTF8;
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Industrial-grade, low-noise, UTF-8-safe logging.
    /// Development → Console + Debug + File
    /// Production  → Console(JSON) + Rolling Files
    /// </summary>
    public static IHostBuilder UseLog(this IHostBuilder hostBuilder)
    {
      TraceHelper.LogConsole();

      hostBuilder.UseSerilog((context, services, logCfg) =>
      {
        var env = context.HostingEnvironment;
        var utf8 = new UTF8Encoding(false);

        logCfg
          .ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithProperty("Application", env.ApplicationName)
          .Enrich.WithMachineName()
          .Enrich.WithEnvironmentName()
           // ---- Global noise suppression ----
          .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"))
          .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Database.Command"))
          .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Infrastructure"))
          .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient"))
          .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.DataProtection"))
          .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.StaticFiles"))
          .Filter.ByExcluding(Matching.FromSource("Microsoft.Hosting.Lifetime"))
          .Filter.ByExcluding(Matching.FromSource("Microsoft.Extensions.Diagnostics.HealthChecks"));

        if (env.IsDevelopment())
        {
          logCfg
            .MinimumLevel.Debug()
            .WriteTo.Console(
              outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
              restrictedToMinimumLevel: LogEventLevel.Debug)
            .WriteTo.Debug()
            .WriteTo.File(
              path: "logs/dev-.log",
              rollingInterval: RollingInterval.Day,
              encoding: utf8,
              retainedFileCountLimit: 10);
        }
        else
        {
          logCfg
            .MinimumLevel.Information()
            .WriteTo.Console(
              outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
              restrictedToMinimumLevel: LogEventLevel.Information)
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
      hostBuilder.ConfigureServices((_, __) => Agent.Setup(new AgentComponents()));
#endif
      return hostBuilder;
    }

    /// <summary>
    /// Hybrid (config-driven) logging with UTF-8 enforcement and noise suppression.
    /// </summary>
    public static IHostBuilder UseHybridLog(this IHostBuilder hostBuilder)
    {
      TraceHelper.LogConsole();
      Console.OutputEncoding = Encoding.UTF8;

      hostBuilder.UseSerilog((context, services, logCfg) =>
      {
        var env = context.HostingEnvironment;

        logCfg
          .ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithProperty("Application", env.ApplicationName)
          .Enrich.WithMachineName()
          .Enrich.WithEnvironmentName()
          .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"))
          .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Database.Command"))
          .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient"));
      });

#if DEBUG
      hostBuilder.ConfigureServices((_, __) => Agent.Setup(new AgentComponents()));
#endif
      return hostBuilder;
    }
  }
}
