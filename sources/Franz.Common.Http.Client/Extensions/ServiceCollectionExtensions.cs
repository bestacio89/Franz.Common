using Franz.Common;
using Franz.Common.Errors;
using Franz.Common.Http.Client;
using Franz.Common.Http.Client.Delegating;
using Franz.Common.Http.Client.Properties;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddHttpServices(this IServiceCollection services, IConfiguration configuration, TimeSpan timeout, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    assemblyPredicate ??= (Assembly assembly) =>
    {
      var result = (assembly.GetName().Name?.StartsWith(Company.Name, StringComparison.InvariantCulture) == true ||
      assembly.GetName().Name?.StartsWith(ProductGeneration.Name, StringComparison.InvariantCulture) == true) &&
      assembly.GetName().Name?.EndsWith(".ClientHttp", StringComparison.InvariantCulture) == true;

      return result;
    };

    services.AddMatchingInterfaceScoped<HttpService>(assemblyPredicate);

    ReflectionHelper.GetCurrentAppDomainAssemblies(assemblyPredicate)
      .ToList()
      .ForEach(assembly =>
      {
        AddHttpClientNamed(services, configuration, timeout, assembly);
      });

    services
      .AddSerializers()
      .AddNoDuplicateTransient<HttpClient>()
      .AddDelegatingHandlers();

    return services;
  }

  private static IServiceCollection AddHttpClientNamed(this IServiceCollection services, IConfiguration configuration, TimeSpan timeout, Assembly assembly)
  {
    var namespaceName = string.Join(".", assembly.GetName()!.Name!.Split(".").Reverse().Skip(1).Reverse());

    services
      .AddHttpClient(namespaceName, httpClient =>
      {
        httpClient = ConfigureBaseAddress(httpClient, configuration, namespaceName);
        httpClient.Timeout = timeout;
        httpClient = ConfigureTimeout(httpClient, configuration, namespaceName);
      })
      .AddHttpMessageHandler<ExceptionDelegatingHandler>()
      .AddHttpMessageHandler<RequestBuilderDelegatingHandler>();

    return services;
  }

  private static HttpClient ConfigureBaseAddress(HttpClient httpClient, IConfiguration configuration, string namespaceName)
  {
    var baseAddressConfiguration = $"HttpServices:{namespaceName}:BaseAddress";
    var baseAddress = configuration.GetValue<string>(baseAddressConfiguration);
    httpClient.BaseAddress = !string.IsNullOrEmpty(baseAddress) && httpClient.BaseAddress?.ToString() != baseAddress
      ? new Uri(baseAddress)
      : throw new TechnicalException(string.Format(Resources.MissingBaseAddressConfiguration, baseAddressConfiguration));

    return httpClient;
  }

  private static HttpClient ConfigureTimeout(HttpClient httpClient, IConfiguration configuration, string namespaceName)
  {
    var timeoutConfiguration = $"HttpServices:{namespaceName}:Timeout";
    var timeoutValue = configuration.GetValue<string>(timeoutConfiguration);
    if (TimeSpan.TryParse(timeoutValue, out var timeout))
      httpClient.Timeout = timeout;

    return httpClient;
  }

  private static IServiceCollection AddDelegatingHandlers(this IServiceCollection services)
  {
    services
      .AddNoDuplicateTransient<ExceptionDelegatingHandler>()
      .AddNoDuplicateTransient<RequestBuilderDelegatingHandler>();

    return services;
  }
}
