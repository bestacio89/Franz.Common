using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Franz.Common.Serialization.Extensions;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSerializers(this IServiceCollection services)
  {
    services
      .AddNoDuplicateSingleton<IJsonSerializer, Franz.Common.Serialization.JsonSerializer>()
      .AddNoDuplicateSingleton<IByteArraySerializer, ByteArraySerializer>()
      .AddInheritedClassSingleton<JsonConverter>();

    return services;
  }
}
