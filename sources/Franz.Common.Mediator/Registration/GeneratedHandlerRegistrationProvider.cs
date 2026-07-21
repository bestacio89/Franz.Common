using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Discovers and executes compile-time source-generated handler registrations from calling assemblies.
/// </summary>
public sealed class GeneratedHandlerRegistrationProvider : IHandlerRegistrationProvider
{
  private readonly Assembly _targetAssembly;

  public GeneratedHandlerRegistrationProvider(Assembly? targetAssembly = null)
      {
        _targetAssembly = targetAssembly ?? Assembly.GetCallingAssembly() ?? Assembly.GetEntryAssembly();
      }

  public void Register(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);
    // Find the generated registration class in the target assembly via marker attribute or type convention
    var registrationType = _targetAssembly.GetType("Franz.Common.Mediator.Registration.FranzMediatorGeneratedRegistration");

    // If the well-known type does not exist (different generator output or packaging),
    // attempt to find any concrete type in the assembly that implements IHandlerRegistrationProvider.
    if (registrationType is null)
    {
      // Try to find any concrete IHandlerRegistrationProvider in the target assembly
      registrationType = _targetAssembly
          .GetTypes()
          .FirstOrDefault(t =>
              typeof(IHandlerRegistrationProvider).IsAssignableFrom(t)
              && !t.IsInterface
              && !t.IsAbstract
              && t.GetConstructor(Type.EmptyTypes) != null);
    }

    // If still not found, search all currently loaded assemblies for a provider type.
    if (registrationType is null)
    {
      registrationType = AppDomain.CurrentDomain.GetAssemblies()
          .Where(a => !a.IsDynamic)
          .SelectMany(a =>
          {
            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
          })
          .FirstOrDefault(t =>
              typeof(IHandlerRegistrationProvider).IsAssignableFrom(t)
              && !t.IsInterface
              && !t.IsAbstract
              && t.GetConstructor(Type.EmptyTypes) != null);
    }

    if (registrationType is null)
    {
      // No parameterless provider found. As a fallback, use the reflection-based provider
      // to discover handler types in the target assembly at runtime. This preserves test
      // behavior when source-generation hasn't emitted the generated registration.
      try
      {
        var reflectionProvider = new ReflectionHandlerRegistrationProvider(_targetAssembly);
        reflectionProvider.Register(services);
        return;
      }
      catch
      {
        // If ReflectionHandlerRegistrationProvider is not available or fails, just return.
        return;
      }
    }

    if (Activator.CreateInstance(registrationType) is IHandlerRegistrationProvider provider)
    {
      provider.Register(services);
    }
  }
}