using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Resolves the generated handler-registration provider for one explicitly selected application assembly.
/// If the generator output is absent, falls back to reflection over that same assembly for compatibility.
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

    // The generator's well-known type is the deterministic fast path.
    var registrationType = _targetAssembly.GetType(
        "Franz.Common.Mediator.Registration.FranzMediatorGeneratedRegistration",
        throwOnError: false,
        ignoreCase: false);

    // Preserve compatibility with custom providers emitted into the target assembly.
    if (registrationType is null)
    {
      registrationType = _targetAssembly
          .GetTypes()
          .FirstOrDefault(type =>
              typeof(IHandlerRegistrationProvider).IsAssignableFrom(type)
              && !type.IsInterface
              && !type.IsAbstract
              && type.GetConstructor(Type.EmptyTypes) is not null);
    }

    if (registrationType is not null)
    {
      if (Activator.CreateInstance(registrationType) is not IHandlerRegistrationProvider provider)
      {
        throw new InvalidOperationException(
            $"Handler registration provider '{registrationType.FullName}' could not be instantiated.");
      }

      provider.Register(services);
      return;
    }

    // Compatibility fallback is intentionally restricted to the requested assembly.
    // Never scan unrelated loaded assemblies: registration must be scoped to the caller's
    // explicit composition boundary and must not depend on load order.
    new ReflectionHandlerRegistrationProvider(_targetAssembly).Register(services);
  }
}
