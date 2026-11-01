using Franz.Common.Mediator.Registration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Franz.Common.Business.Helpers
{
  /// <summary>
  /// Retains the legacy transient handler-collection contract while delegating
  /// discovery to compile-time generated registrations.
  /// </summary>
  public static class HandlerCollector
  {
    public static void CollectHandlers(IServiceCollection services, Assembly assembly)
    {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(assembly);

      GeneratedHandlerRegistrationRegistry.ApplyTransient(assembly, services);
    }
  }
}
