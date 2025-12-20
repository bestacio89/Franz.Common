using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Errors;
using Franz.Common.Headers;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Properties;
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


  public static IServiceCollection AddMessagingFactories(this IServiceCollection services)
  {
    services.AddNoDuplicateSingleton<IMessageFactory, MessageFactory>();

    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, CommandMessageBuilderStrategy>();
    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, QueryMessageBuilderStrategy>();
    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, IntegrationEventMessageBuilderStrategy>();
    services.AddNoDuplicateSingleton<IMessageBuilderStrategy, ExecutionFaultMessageBuilderStrategy>();

    return services;
  }


#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddMessagingOptions(this IServiceCollection services, IConfiguration? configuration)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var configurationSection = configuration?.GetSection("Messaging");
    var hasMessagingConnectionOptions = services.Any(service => service.ServiceType == typeof(IConfigureOptions<MessagingOptions>));

    if (configurationSection?.Exists() == true && !hasMessagingConnectionOptions)
    {
      services
        .AddOptions()
        .Configure<MessagingOptions>(configurationSection);
    }
    else if (!hasMessagingConnectionOptions)
    {
      throw new TechnicalException(Resources.MessagingNoConfigurationException);
    }

    return services;
  }
}
