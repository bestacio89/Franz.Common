using AutoMapper;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddAutoMapper(this IServiceCollection services)
  {
    services
      .AddInheritedClassSingleton<Profile>()
      .AddNoDuplicateSingleton(serviceProvider =>
      {
        var profiles = serviceProvider.GetServices<Profile>();

        var mapperConfiguration = new MapperConfiguration(configuration =>
        {
          configuration.AddProfiles(profiles);
        });
        var result = mapperConfiguration.CreateMapper();

        return result;
      });

    return services;
  }
}
