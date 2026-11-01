using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Applies compile-time generated handler registrations for one explicitly selected application assembly.
/// </summary>
public sealed class GeneratedHandlerRegistrationProvider : IHandlerRegistrationProvider
{
  private readonly Assembly _targetAssembly;

  public GeneratedHandlerRegistrationProvider(Assembly? targetAssembly = null)
  {
    _targetAssembly = targetAssembly ?? Assembly.GetCallingAssembly();
  }

  public void Register(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);
    GeneratedHandlerRegistrationRegistry.Apply(_targetAssembly, services);
  }
}
