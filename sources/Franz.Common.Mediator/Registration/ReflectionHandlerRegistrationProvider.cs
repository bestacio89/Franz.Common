using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Compatibility type retained for existing source and binary references.
/// Handler discovery is compile-time generated; this provider never scans assemblies.
/// </summary>
public sealed class ReflectionHandlerRegistrationProvider : IHandlerRegistrationProvider
{
  private readonly Assembly[] _assemblies;

  public ReflectionHandlerRegistrationProvider(params Assembly[] assemblies)
  {
    ArgumentNullException.ThrowIfNull(assemblies);
    _assemblies = assemblies;
  }

  public void Register(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    for (var index = 0; index < _assemblies.Length; index++)
    {
      GeneratedHandlerRegistrationRegistry.Apply(_assemblies[index], services);
    }
  }
}
