using System;
using System.Text;
using Franz.Common.Logging.Tracing;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
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

        private static LoggerConfiguration ApplyBaseFilters(this LoggerConfiguration cfg)
        {
            return cfg
                // Framework Internals
                .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Database.Command"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Infrastructure"))
                .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.DataProtection"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.StaticFiles"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.Hosting.Lifetime"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.Extensions.Diagnostics.HealthChecks"))
                // Boot Chatter
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Watch"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.WebTools"))
                // Kestrel Transport
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel.Connections"))
                // Routing & Binding
                .Filter.ByExcluding(e => e.Level < LogEventLevel.Warning && Matching.FromSource("Microsoft.AspNetCore.Routing")(e))
                .Filter.ByExcluding(e => e.Level < LogEventLevel.Warning && Matching.FromSource("Microsoft.AspNetCore.Mvc.ModelBinding")(e))
                .Filter.ByExcluding(e => e.Level < LogEventLevel.Warning && Matching.FromSource("Asp.Versioning")(e));
        }

        public static IHostBuilder UseLog(this IHostBuilder hostBuilder)
        {
            TraceHelper.LogConsole();
            hostBuilder.UseSerilog((context, services, logCfg) =>
            {
                var env = context.HostingEnvironment;
                var utf8 = new UTF8Encoding(false);

                logCfg.ApplyBaseFilters()
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", env.ApplicationName)
                    .Enrich.WithMachineName()
                    .Enrich.WithEnvironmentName();

                if (env.IsDevelopment())
                {
                    logCfg.MinimumLevel.Debug()
                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | ReqId:{RequestId} | {Message:lj}{NewLine}{Exception}")
                        .WriteTo.Debug()
                        .WriteTo.File("logs/dev-.log", rollingInterval: RollingInterval.Day, encoding: utf8, retainedFileCountLimit: 10);
                }
                else
                {
                    logCfg.MinimumLevel.Information()
                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | ReqId:{RequestId} | {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/prod-sre-.json", rollingInterval: RollingInterval.Day, encoding: utf8)
                        .WriteTo.File("logs/prod-dev-.log", rollingInterval: RollingInterval.Day, encoding: utf8, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext} | ReqId:{RequestId} | Path:{RequestPath} | {Message:lj}{NewLine}{Exception}");
                }
            });

#if DEBUG
            hostBuilder.ConfigureServices((_, __) => Agent.Setup(new AgentComponents()));
#endif
            return hostBuilder;
        }

        public static IHostBuilder UseDesktopLog(this IHostBuilder hostBuilder)
        {
            TraceHelper.LogConsole();
            
            hostBuilder.UseSerilog((context, services, logCfg) =>
            {
                logCfg.ApplyBaseFilters()
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId(); // Contextual ThreadID for UI debugging

                // Desktop-specific template (No ReqId/Path)
                const string desktopTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | Thread:{ThreadId} | {Message:lj}{NewLine}{Exception}";

                logCfg.MinimumLevel.Debug()
                      .WriteTo.Console(outputTemplate: desktopTemplate)
                      .WriteTo.Debug()
                      .WriteTo.File("logs/desktop-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10);
            });

#if DEBUG
            hostBuilder.ConfigureServices((_, __) => Agent.Setup(new AgentComponents()));
#endif
            return hostBuilder;
        }

        public static IHostBuilder UseHybridLog(this IHostBuilder hostBuilder)
        {
            TraceHelper.LogConsole();
            hostBuilder.UseSerilog((context, services, logCfg) =>
            {
                logCfg.ApplyBaseFilters()
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                    .Enrich.WithMachineName();
            });
            return hostBuilder;
        }
    }
}