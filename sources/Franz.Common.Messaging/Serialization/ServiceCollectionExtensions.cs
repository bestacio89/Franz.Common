using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Serialization;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddDefaultMessageSerializer(this IServiceCollection services)
  {
    services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
    return services;
  }
}
