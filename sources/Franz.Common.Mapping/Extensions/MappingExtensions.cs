using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Mapping.Extensions;

public static class MappingExtensions
{
  [RequiresUnreferencedCode("Assembly profile discovery requires reflection. Register profiles explicitly for trimmed applications.")]
  [RequiresDynamicCode("Assembly profile discovery activates profile types at runtime. Register profiles explicitly for Native AOT.")]
  public static IServiceCollection AddFranzMapping(
    this IServiceCollection services,
    Action<MappingConfiguration>? configure = null,
    params Assembly[] assemblies)
  {
    var config = new MappingConfiguration();
    configure?.Invoke(config);

    var scanAssemblies = assemblies.Length > 0
        ? assemblies
        : ReflectionHelper.GetCurrentAppDomainAssemblies(
            ReflectionHelper.GetAssemblyCompanyOrProductPredicate());

    // =========================================================
    // PROFILE DISCOVERY (CORE OF YOUR REQUEST)
    // =========================================================
    var profiles = scanAssemblies
        .SelectMany(a => a.GetTypes())
        .Where(t =>
            typeof(IFranzMapProfile).IsAssignableFrom(t) &&
            !t.IsAbstract &&
            !t.IsInterface)
        .Distinct()
        .Select(t => (IFranzMapProfile)Activator.CreateInstance(t)!)
        .ToList();

    foreach (var profile in profiles)
    {
      profile.Configure(config);
    }

    // =========================================================
    // CORE SERVICES ONLY (NO OPEN GENERICS)
    // =========================================================
    services.AddSingleton(config);
    services.AddSingleton<IFranzMapper, FranzMapper>();
    services.AddSingleton<IMappingService, MappingService>();

    return services;
  }

  [RequiresUnreferencedCode("Assembly profile discovery requires reflection. Register profiles explicitly for trimmed applications.")]
  [RequiresDynamicCode("Assembly profile discovery activates profile types at runtime. Register profiles explicitly for Native AOT.")]
  public static IServiceCollection AddFranzMapping(
      this IServiceCollection services,
      params Assembly[] assemblies)
  {
    return services.AddFranzMapping(null, assemblies);
  }

  // =========================================================
  // Profile loading
  // =========================================================

}