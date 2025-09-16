using Franz.Common.Logging.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

#if DEBUG
using Elastic.Apm; // gives you Agent
using Elastic.Apm.NetCoreAll;
#endif

namespace Microsoft.Extensions.Hosting
{
  public static class HostBuilderExtensions
  {
    public static IHostBuilder UseLog(this IHostBuilder hostBuilder)
    {
      TraceHelper.LogConsole();

      // Configure Serilog
      hostBuilder.UseSerilog((context, services, config) =>
      {
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console();
      });

#if DEBUG
      // Elastic APM diagnostics only when debugging
      hostBuilder.ConfigureServices((context, services) =>
      {
        // Agent setup via environment variables or configuration
        Agent.Setup(new AgentComponents());
      });
#endif

      return hostBuilder;
    }
  }
}
