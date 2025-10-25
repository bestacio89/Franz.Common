using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace Franz.Common.Mapping.Extensions;
public static class MappingServiceCollectionExtensions
{
  public static IServiceCollection AddFranzMapping(
      this IServiceCollection services,
      Action<MappingConfiguration>? configure = null)
  {
    var config = new MappingConfiguration();

    configure?.Invoke(config);

    services.AddSingleton(config);
    services.AddSingleton<IFranzMapper, FranzMapper>();

    return services;
  }

  public static IServiceCollection AddFranzMapping(
       this IServiceCollection services,
       Action<MappingConfiguration> configure,
       params Assembly[] assemblies)
  {
    var config = new MappingConfiguration();

    // ✅ run user-provided config
    configure(config);

    // ✅ scan for profiles in the given assemblies
    foreach (var assembly in assemblies)
    {
      AddProfilesFromAssembly(config, assembly);
    }

    services.AddSingleton(config);
    services.AddSingleton<IFranzMapper, FranzMapper>();

    return services;
  }
  public static IServiceCollection AddFranzMapping(
          this IServiceCollection services,
          params Assembly[] assemblies)
  {
    // Reuse the main overload with a no-op config
    return services.AddFranzMapping(_ => { }, assemblies);
  }
  public static void AddProfilesFromAssembly(this MappingConfiguration config, Assembly assembly)
  {
    var profiles = assembly.GetTypes()
        .Where(t => typeof(IFranzMapProfile).IsAssignableFrom(t) && !t.IsAbstract)
        .Select(Activator.CreateInstance)
        .Cast<IFranzMapProfile>();

    foreach (var profile in profiles)
      profile.Configure(config);
  }
}
