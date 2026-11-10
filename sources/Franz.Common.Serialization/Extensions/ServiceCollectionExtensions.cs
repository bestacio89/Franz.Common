using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSerializers(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services
      .AddNoDuplicateSingleton<IJsonSerializer, SystemTextJsonSerializer>()
      .AddNoDuplicateSingleton<IByteArraySerializer, ByteArraySerializer>();

    return services;
  }

  /// <summary>
  /// Registers serializers using application-owned source-generated JSON metadata.
  /// This is the required registration path for Native AOT applications.
  /// </summary>
  public static IServiceCollection AddSerializers(
      this IServiceCollection services,
      JsonSerializerContext jsonSerializerContext)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(jsonSerializerContext);

    services.TryAddSingleton<JsonSerializerContext>(jsonSerializerContext);
    return services.AddSerializers();
  }
}