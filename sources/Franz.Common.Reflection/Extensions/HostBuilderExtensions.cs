using System.Reflection;

namespace Microsoft.Extensions.Hosting;
public static class HostBuilderExtensions
{
  public static IHostBuilder LoadAssemblyReferencedNotLoaded(this IHostBuilder hostBuilder, Assembly entryAssembly)
  {
    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
      .Where(assembly => !assembly.IsDynamic)
      .ToList();
    var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();

    var productName = string.Join(".", entryAssembly!.GetName().Name!.Split(".").Take(2));
    var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, $"{productName}*.dll", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });
    var toLoadAssemblies = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();

    toLoadAssemblies.ForEach(path =>
    {
      loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
    });

    return hostBuilder;
  }
}
