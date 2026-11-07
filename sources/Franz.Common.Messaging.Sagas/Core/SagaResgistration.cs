#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Stores metadata about saga-message relationships.
/// Useful for routing, validation, and orchestrator dispatch.
/// </summary>
public sealed class SagaRegistration
{
  public Type SagaType { get; }
  public Type StateType { get; }

  public readonly Dictionary<Type, MethodInfo> StartHandlers = new();
  public readonly Dictionary<Type, MethodInfo> StepHandlers = new();
  public readonly Dictionary<Type, MethodInfo> CompensationHandlers = new();

  public SagaRegistration(Type sagaType, Type stateType)
  {
    SagaType = sagaType;
    StateType = stateType;
  }

  public static SagaRegistration FromType(Type sagaType)
  {
    ArgumentNullException.ThrowIfNull(sagaType);

    var sagaInterface = sagaType.GetInterfaces()
      .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISaga<>))
      ?? throw new SagaConfigurationError(
          $"Saga '{sagaType.FullName}' must implement ISaga<TState>.");

    var stateType = sagaInterface.GetGenericArguments()[0];
    var registration = new SagaRegistration(sagaType, stateType);

    RegisterHandlers(sagaType, registration, typeof(IStartWith<>), registration.StartHandlers);
    RegisterHandlers(sagaType, registration, typeof(IHandle<>), registration.StepHandlers);
    RegisterHandlers(sagaType, registration, typeof(ICompensateWith<>), registration.CompensationHandlers);

    return registration;
  }

  private static void RegisterHandlers(
      Type sagaType,
      SagaRegistration registration,
      Type handlerContractDefinition,
      IDictionary<Type, MethodInfo> destination)
  {
    foreach (var contract in sagaType.GetInterfaces())
    {
      if (!contract.IsGenericType ||
          contract.GetGenericTypeDefinition() != handlerContractDefinition)
        continue;

      var messageType = contract.GetGenericArguments()[0];
      destination.Add(messageType, ResolveHandlerMethod(sagaType, contract));
    }
  }

  private static MethodInfo ResolveHandlerMethod(Type sagaType, Type contract)
  {
    var contractMethod = contract.GetMethod(nameof(IStartWith<object>.HandleAsync))
        ?? throw new SagaConfigurationError(
            $"Handler contract '{contract.FullName}' does not expose HandleAsync.");

    var interfaceMap = sagaType.GetInterfaceMap(contract);

    for (var index = 0; index < interfaceMap.InterfaceMethods.Length; index++)
    {
      if (interfaceMap.InterfaceMethods[index] == contractMethod)
        return interfaceMap.TargetMethods[index];
    }

    throw new SagaConfigurationError(
        $"Saga '{sagaType.FullName}' does not provide an implementation for '{contract.FullName}.HandleAsync'.");
  }

  public bool CanStartWith(Type messageType) => StartHandlers.ContainsKey(messageType);
  public bool CanHandle(Type messageType) => StepHandlers.ContainsKey(messageType);
  public bool CanCompensate(Type messageType) => CompensationHandlers.ContainsKey(messageType);

  public IEnumerable<Type> AllMessageTypes =>
      StartHandlers.Keys
          .Concat(StepHandlers.Keys)
          .Concat(CompensationHandlers.Keys)
          .Distinct();
}
