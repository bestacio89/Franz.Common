using System.Text.Json.Serialization;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessagingHeaderContext(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);
    services.AddNoDuplicateScoped<IHeaderContextAccessor, HeaderContextAccessor>();
    return services;
  }

  public static IServiceCollection AddMessagingSerialization(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    if (!services.Any(static descriptor => descriptor.ServiceType == typeof(IMessageSerializer)))
    {
      services.AddScoped<IMessageSerializer>(serviceProvider =>
      {
        var jsonSerializerContext = serviceProvider.GetService<JsonSerializerContext>();
        return jsonSerializerContext is null
            ? new JsonMessageSerializer()
            : new JsonMessageSerializer(jsonSerializerContext);
      });
    }

    return services;
  }

  /// <summary>Registers messaging serialization with application-owned source-generated JSON metadata.</summary>
  public static IServiceCollection AddMessagingSerialization(
      this IServiceCollection services,
      JsonSerializerContext jsonSerializerContext)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(jsonSerializerContext);

    if (!services.Any(static descriptor => descriptor.ServiceType == typeof(JsonSerializerContext)))
      services.AddSingleton<JsonSerializerContext>(jsonSerializerContext);

    return services.AddMessagingSerialization();
  }

  public static IServiceCollection AddMessagingFactories(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);
    services.AddMessagingSerialization();

    RegisterStrategy<CommandMessageBuilderStrategy>(
        services,
        static serviceProvider => new CommandMessageBuilderStrategy(
            serviceProvider.GetRequiredService<IMessageSerializer>()));

    RegisterStrategy<QueryMessageBuilderStrategy>(
        services,
        static serviceProvider => new QueryMessageBuilderStrategy(
            serviceProvider.GetRequiredService<IMessageSerializer>()));

    RegisterStrategy<IntegrationEventMessageBuilderStrategy>(
        services,
        static serviceProvider => new IntegrationEventMessageBuilderStrategy(
            serviceProvider.GetRequiredService<IMessageSerializer>()));

    RegisterStrategy<ExecutionFaultMessageBuilderStrategy>(
        services,
        static serviceProvider => new ExecutionFaultMessageBuilderStrategy(
            serviceProvider.GetRequiredService<IMessageSerializer>()));

    if (!services.Any(static descriptor => descriptor.ServiceType == typeof(IMessageFactory)))
    {
      services.AddScoped<IMessageFactory>(
          serviceProvider => new MessageFactory(
              serviceProvider.GetServices<IMessageBuilderStrategy>()));
    }

    return services;
  }

  private static void RegisterStrategy<TStrategy>(
      IServiceCollection services,
      Func<IServiceProvider, TStrategy> factory)
      where TStrategy : class, IMessageBuilderStrategy
  {
    if (services.Any(descriptor => descriptor.ServiceType == typeof(TStrategy)))
      return;

    services.AddScoped(factory);
    services.AddScoped<IMessageBuilderStrategy>(
        serviceProvider => serviceProvider.GetRequiredService<TStrategy>());
  }
}
