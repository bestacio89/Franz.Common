using Microsoft.Extensions.Hosting;

namespace Franz.Common.Hosting.Extensions;
public static class HostExtensions
{
  public static async Task<IHost> Initialize(this IHost host)
  {
    ArgumentNullException.ThrowIfNull(host);

    await host.Services.InitializeAsync().ConfigureAwait(false);

    return host;
  }
}