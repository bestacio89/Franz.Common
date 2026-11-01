using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Reflection.Extensions;

public static class HostBuilderExtensions
{
  [RequiresUnreferencedCode("Loading assemblies by scanning the application directory is not compatible with trimming.")]
  [RequiresDynamicCode("Loading assemblies at runtime is not compatible with Native AOT.")]
  public static IHostBuilder LoadAssemblyReferencedNotLoaded(this IHostBuilder hostBuilder, Assembly entryAssembly)
  {
    ArgumentNullException.ThrowIfNull(hostBuilder);
    ArgumentNullException.ThrowIfNull(entryAssembly);

    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
      .Where(assembly => !assembly.IsDynamic)
      .ToList();
    var loadedNames = loadedAssemblies
      .Select(assembly => assembly.GetName().Name)
      .Where(static name => name is not null)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var productName = string.Join(".", entryAssembly.GetName().Name!.Split(".").Take(2));
    var referencedPaths = Directory.GetFiles(
      AppContext.BaseDirectory,
      $"{productName}*.dll",
      new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

    var toLoadAssemblies = referencedPaths
      .Where(path => !loadedNames.Contains(Path.GetFileNameWithoutExtension(path)))
      .ToList();

    foreach (var path in toLoadAssemblies)
      loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));

    return hostBuilder;
  }
}
