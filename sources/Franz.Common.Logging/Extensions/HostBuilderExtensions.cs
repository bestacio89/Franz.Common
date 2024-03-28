using Franz.Common.Logging.Tracing;
#if !DEBUG
using Elastic.Apm.NetCoreAll;
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
    hostBuilder.UseAllElasticApm();
#endif

    return hostBuilder;
  }
}
