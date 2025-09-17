using Franz.Common.Business.Helpers;
using Franz.Common.Business.Properties;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Franz.Common.Business.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddBusinessWithMediator(
      this IServiceCollection services,
      Assembly entryAssembly,
      Action<FranzMediatorOptions>? configure = null)
  {
    var productName = string.Join(".", entryAssembly!.GetName().Name!.Split(".").Take(2));
    var applicationAssemblyName = $"{productName}.Application";

    var applicationAssembly = SearchApplicationAssemblyInCurrentAppDomain(applicationAssemblyName)
        ?? throw new TechnicalException(Resources.ApplicationDependencyNotExistsException);

    // 🔹 Forward into Franz.Common.Mediator
    services.AddFranzMediator(new[] { applicationAssembly }, configure);

    // 🔹 Collect handlers (domain + application layer)
    HandlerCollector.CollectHandlers(services, applicationAssembly);

    return services;
  }

  private static Assembly? SearchApplicationAssemblyInCurrentAppDomain(string applicationAssemblyName)
  {
    return AppDomain.CurrentDomain
        .GetAssemblies()
        .FirstOrDefault(assembly =>
            assembly.GetName().Name is not null &&
            assembly.GetName().Name.Equals(applicationAssemblyName, StringComparison.InvariantCultureIgnoreCase));
  }
}
