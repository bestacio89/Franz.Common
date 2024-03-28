namespace Microsoft.Extensions.Hosting;

public static class HostExtensions
{
  public static IHost Initialize(this IHost host)
  {
    host.Services.Initialize();

    return host;
  }
}