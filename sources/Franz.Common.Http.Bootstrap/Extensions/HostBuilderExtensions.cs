using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IHostBuilder UseHttp<TStartup>(this IHostBuilder hostBuilder, Assembly? assembly = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
      where TStartup : class
  {
    if (assembly == null)
      assembly = Assembly.GetCallingAssembly();

    var result = hostBuilder
      .LoadAssemblyReferencedNotLoaded(assembly)
      .UseLog()
      .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<TStartup>());

    return result;
  }
}