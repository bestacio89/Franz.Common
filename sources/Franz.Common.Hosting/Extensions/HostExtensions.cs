using Microsoft.Extensions.Hosting;

namespace Franz.Common.Hosting.Extensions;
public static class HostExtensions
{
  public static IHost Initialize(this IHost host)
  {
    host.Services.Initialize();

    return host;
  }
}