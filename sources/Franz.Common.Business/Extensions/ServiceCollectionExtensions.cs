using Franz.Common.Business.Helpers;
using Franz.Common.Business.Properties;
using Franz.Common.Errors;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMediator(this IServiceCollection services, Assembly entryAssembly)
  {
    var productName = string.Join(".", entryAssembly!.GetName().Name!.Split(".").Take(2));
    var applicationAssemblyName = $"{productName}.Application";

    var applicationAssembly = SearchApplicationAssemblyInCurrentAppDomain(applicationAssemblyName) ?? throw new TechnicalException(Resources.ApplicationDependencyNotExistsException);
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
    HandlerCollector.CollectHandlers(services, applicationAssembly);
    return services;
  }


  private static Assembly SearchApplicationAssemblyInCurrentAppDomain(string applicationAssemblyName)

  {
    return AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(assembly => assembly.GetName().Name is not null && assembly.GetName().Name!.Equals(applicationAssemblyName, StringComparison.InvariantCultureIgnoreCase))
        .SingleOrDefault();
  }
}
