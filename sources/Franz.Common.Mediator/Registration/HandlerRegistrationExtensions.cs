using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Franz.Common.Mediator.Registration;

public static class HandlerRegistrationExtensions
{
  public static IServiceCollection AddFranzHandlerRegistration(
      this IServiceCollection services,
      params Assembly[] assemblies)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(assemblies);

    for (var index = 0; index < assemblies.Length; index++)
    {
      GeneratedHandlerRegistrationRegistry.Apply(assemblies[index], services);
    }

    return services;
  }
}
