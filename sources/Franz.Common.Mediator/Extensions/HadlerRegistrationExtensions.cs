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

    var provider =
        new ReflectionHandlerRegistrationProvider(assemblies);

    provider.Register(services);

    return services;
  }
}