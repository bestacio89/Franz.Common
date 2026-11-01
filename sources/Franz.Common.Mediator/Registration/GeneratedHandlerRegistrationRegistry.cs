using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Franz.Common.Mediator.Registration;

/// <summary>
/// Stores compile-time generated handler registrations contributed by application modules.
/// </summary>
public static class GeneratedHandlerRegistrationRegistry
{
  private static readonly ConcurrentDictionary<Assembly, Action<IServiceCollection>> Registrations = new();
  private static readonly ConcurrentDictionary<Assembly, Action<IServiceCollection>> TransientRegistrations = new();
  private static readonly ConcurrentDictionary<Assembly, Action<IServiceCollection>> DefaultPipelineRegistrations = new();

  /// <summary>
  /// Registers the generated contribution for the assembly containing the generated module.
  /// </summary>
  public static void Register(Assembly assembly, Action<IServiceCollection> registration)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(registration);

    if (!Registrations.TryAdd(assembly, registration))
    {
      throw new InvalidOperationException(
          $"A generated mediator registration contribution is already registered for assembly '{assembly.GetName().Name}'.");
    }
  }

  /// <summary>
  /// Registers the generated transient contribution for legacy handler collection APIs.
  /// </summary>
  public static void RegisterTransient(Assembly assembly, Action<IServiceCollection> registration)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(registration);

    if (!TransientRegistrations.TryAdd(assembly, registration))
    {
      throw new InvalidOperationException(
          $"A generated transient mediator contribution is already registered for assembly '{assembly.GetName().Name}'.");
    }
  }

  /// <summary>
  /// Registers the generated closed-generic default pipeline contribution for an assembly.
  /// </summary>
  public static void RegisterDefaultPipelines(Assembly assembly, Action<IServiceCollection> registration)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(registration);

    if (!DefaultPipelineRegistrations.TryAdd(assembly, registration))
    {
      throw new InvalidOperationException(
          $"A generated default-pipeline contribution is already registered for assembly '{assembly.GetName().Name}'.");
    }
  }

  /// <summary>
  /// Applies generated closed-generic default pipelines for one explicitly selected assembly.
  /// </summary>
  public static void ApplyDefaultPipelines(Assembly assembly, IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(services);

    if (!DefaultPipelineRegistrations.TryGetValue(assembly, out var registration))
    {
      throw new InvalidOperationException(
          $"Assembly '{assembly.GetName().Name}' has no generated Franz Mediator default-pipeline contribution. " +
          "Reference the Franz Mediator source generator in the project that contains the handlers. " +
          "Open-generic pipeline registrations are not supported by Native AOT.");
    }

    registration(services);
  }

  /// <summary>
  /// Applies generated registrations for one explicitly selected assembly.
  /// </summary>
  public static void Apply(Assembly assembly, IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(services);

    if (!Registrations.TryGetValue(assembly, out var registration))
    {
      throw new InvalidOperationException(
          $"Assembly '{assembly.GetName().Name}' has no generated Franz Mediator registration contribution. " +
          "Reference the Franz Mediator source generator in the project that contains the handlers. " +
          "Runtime reflection discovery is not supported.");
    }

    registration(services);
  }

  /// <summary>
  /// Applies generated transient registrations for one explicitly selected assembly.
  /// </summary>
  public static void ApplyTransient(Assembly assembly, IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(services);

    if (!TransientRegistrations.TryGetValue(assembly, out var registration))
    {
      throw new InvalidOperationException(
          $"Assembly '{assembly.GetName().Name}' has no generated Franz Mediator registration contribution. " +
          "Reference the Franz Mediator source generator in the project that contains the handlers. " +
          "Runtime reflection discovery is not supported.");
    }

    registration(services);
  }
}
