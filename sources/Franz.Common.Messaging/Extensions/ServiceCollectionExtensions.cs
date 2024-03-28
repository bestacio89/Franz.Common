using Franz.Common.Errors;
using Franz.Common.Headers;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessagingHeaderContext(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<IHeaderContextAccessor, HeaderContextAccessor>();

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
