using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
