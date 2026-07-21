using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Validation;
using Franz.Common.Mediator.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Franz.Common.Mediator.Registration;

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

    for (var a = 0; a < _assemblies.Length; a++)
    {
      var types = _assemblies[a].GetTypes();

      for (var t = 0; t < types.Length; t++)
      {
        var type = types[t];

        if (type.IsAbstract ||
            type.IsInterface ||
            type.IsGenericTypeDefinition)
        {
          continue;
        }

        RegisterType(services, type);
      }
    }
  }


  private static void RegisterType(
      IServiceCollection services,
      [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type type)
  {
    var interfaces = type.GetInterfaces();

    for (var i = 0; i < interfaces.Length; i++)
    {
      var serviceType = interfaces[i];

      if (!serviceType.IsGenericType)
        continue;

      var definition = serviceType.GetGenericTypeDefinition();

      if (!IsMediatorHandler(definition))
        continue;

      services.TryAddEnumerable(
          ServiceDescriptor.Scoped(
              serviceType,
              type));
    }
  }


  private static bool IsMediatorHandler(Type type)
  {
    return type == typeof(ICommandHandler<,>)
        || type == typeof(IQueryHandler<,>)
        || type == typeof(INotificationHandler<>)
        || type == typeof(IStreamQueryHandler<,>)
        || type == typeof(IEventHandler<>)
        || type == typeof(IValidator<>);
  }
}