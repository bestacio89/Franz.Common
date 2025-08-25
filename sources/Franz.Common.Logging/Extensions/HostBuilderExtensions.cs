using Franz.Common.Logging.Tracing;
using Microsoft.Extensions.DependencyInjection;

#if !DEBUG
using Elastic.Apm.NetCoreAll; // can stay if you still need constants/helpers
using Elastic.Apm.Extensions.Hosting; // needed for AddAllElasticApm
#endif
using Serilog;

namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
  public static IHostBuilder UseLog(this IHostBuilder hostBuilder)
  {
    TraceHelper.LogConsole();

    hostBuilder.UseSerilog();

#if !DEBUG
    hostBuilder.ConfigureServices(static (context, services) =>
    {
      // Recommended way: register Elastic APM through DI
      services.AddAllElasticApm();
    });
#endif

    return hostBuilder;
  }
}
