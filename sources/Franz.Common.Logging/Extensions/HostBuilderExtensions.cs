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
    /// Production  → SRE JSON log + Dev human-readable log + Console
    ///
    /// Boot chatter, infrastructure noise, and framework internals are
    /// suppressed at the Franz level — no appsettings configuration needed.
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

            // =========================================================
            // FRAMEWORK INTERNALS — never useful in application logs
            // =========================================================
            .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Database.Command"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Infrastructure"))
            .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.DataProtection"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.StaticFiles"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.Hosting.Lifetime"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.Extensions.Diagnostics.HealthChecks"))
            .Filter.ByExcluding(e =>
              e.Level < LogEventLevel.Warning &&
              Matching.FromSource("Microsoft.AspNetCore.Mvc")(e))

            // =========================================================
            // BOOT CHATTER — VS tooling, browser refresh, hot reload
            // noise that fires before any user request reaches the app
            // =========================================================
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Watch"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.WebTools"))

            // =========================================================
            // KESTREL TRANSPORT — EOF probes, FIN signals, TLS handshake
            // failures on boot are infrastructure noise, not app errors
            // =========================================================
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Connections"))
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Error &&
                Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Https")(e))

            // =========================================================
            // LOCALIZATION — AcceptLanguageHeader noise fires on every
            // request when the browser sends en-US to a French-locale API
            // =========================================================
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Localization"))

            // =========================================================
            // ROUTING & MODEL BINDING — DfaMatcher candidate lists and
            // model binder provider registration spam on every startup
            // =========================================================
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Warning &&
                Matching.FromSource("Microsoft.AspNetCore.Routing")(e))
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Warning &&
                Matching.FromSource("Microsoft.AspNetCore.Mvc.ModelBinding")(e))

            // =========================================================
            // API VERSIONING — description provider execution logs
            // are registration-time noise, not runtime information
            // =========================================================
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Warning &&
                Matching.FromSource("Asp.Versioning")(e));

        if (env.IsDevelopment())
        {
          logCfg
              .MinimumLevel.Debug()
              .WriteTo.Console(
                  outputTemplate:
                      "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | ReqId:{RequestId} | {Message:lj}{NewLine}{Exception}",
                  restrictedToMinimumLevel: LogEventLevel.Debug)
              .WriteTo.Debug()
              .WriteTo.File(
                  path: "logs/dev-.log",
                  rollingInterval: RollingInterval.Day,
                  encoding: utf8,
                  outputTemplate:
                      "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} | ReqId:{RequestId} | Path:{RequestPath} | {Message:lj}{NewLine}{Exception}",
                  retainedFileCountLimit: 10);
        }
        else
        {
          logCfg
              .MinimumLevel.Information()
              .WriteTo.Console(
                  outputTemplate:
                      "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | ReqId:{RequestId} | {Message:lj}{NewLine}{Exception}",
                  restrictedToMinimumLevel: LogEventLevel.Information);

          // 1️⃣ SRE log (JSON, structured)
          logCfg.WriteTo.File(
              path: "logs/prod-sre-.json",
              rollingInterval: RollingInterval.Day,
              retainedFileCountLimit: 30,
              encoding: utf8,
              formatter: new Serilog.Formatting.Json.JsonFormatter());

          // 2️⃣ Dev log (human-readable)
          logCfg.WriteTo.File(
              path: "logs/prod-dev-.log",
              rollingInterval: RollingInterval.Day,
              retainedFileCountLimit: 30,
              encoding: utf8,
              outputTemplate:
                  "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext} | ReqId:{RequestId} | Path:{RequestPath} | {Message:lj}{NewLine}{Exception}");
        }
      });

#if DEBUG
      hostBuilder.ConfigureServices((_, __) => Agent.Setup(new AgentComponents()));
#endif
      return hostBuilder;
    }

    /// <summary>
    /// Hybrid (config-driven) logging with UTF-8 enforcement and noise suppression.
    /// Applies the same boot chatter and framework internal filters as UseLog
    /// so config-driven services get the same clean baseline.
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

            .Filter.ByExcluding(e =>
              e.Level < LogEventLevel.Warning &&
              Matching.FromSource("Microsoft.AspNetCore.Mvc")(e))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Core"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets"))
            // Framework internals
            .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Database.Command"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Infrastructure"))
            .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient"))

            // Boot chatter
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Watch"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.WebTools"))

            // Kestrel transport
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Connections"))
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Error &&
                Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Https")(e))

            // Localization noise
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Localization"))

            // Routing and model binding
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Warning &&
                Matching.FromSource("Microsoft.AspNetCore.Routing")(e))
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Warning &&
                Matching.FromSource("Microsoft.AspNetCore.Mvc.ModelBinding")(e))

            // API versioning
            .Filter.ByExcluding(e =>
                e.Level < LogEventLevel.Warning &&
                Matching.FromSource("Asp.Versioning")(e));
      });

#if DEBUG
      hostBuilder.ConfigureServices((_, __) => Agent.Setup(new AgentComponents()));
#endif
      return hostBuilder;
    }
  }
}