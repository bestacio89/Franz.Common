using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Errors;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Properties;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessagingHeaderContext(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<IHeaderContextAccessor, HeaderContextAccessor>();

    return services;
  }

  public static IServiceCollection AddMessagingSerialization(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<IMessageSerializer, JsonMessageSerializer>();

    return services;
  }

  public static IServiceCollection AddMessagingFactories(this IServiceCollection services)
  {
    services.AddNoDuplicateSingleton<IMessageFactory, MessageFactory>();

    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, CommandMessageBuilderStrategy>();
    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, QueryMessageBuilderStrategy>();
    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, IntegrationEventMessageBuilderStrategy>();
    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, ExecutionFaultMessageBuilderStrategy>();

    return services;
  }


}
