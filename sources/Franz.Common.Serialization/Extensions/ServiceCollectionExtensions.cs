using Franz.Common.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyInjection;
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
