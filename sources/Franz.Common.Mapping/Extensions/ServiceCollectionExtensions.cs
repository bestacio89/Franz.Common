using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Mapping.Extensions
{
  public static class ServiceCollectionExtensions
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
  }
}
