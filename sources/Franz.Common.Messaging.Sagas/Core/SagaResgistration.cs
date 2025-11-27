#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Franz.Common.Messaging.Sagas.Abstractions;

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
    var stateType = sagaType.GetInterfaces()
      .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISaga<>))
      .GetGenericArguments()[0];

    var registration = new SagaRegistration(sagaType, stateType);

    var methods = sagaType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    foreach (var iface in sagaType.GetInterfaces())
    {
      if (iface.IsGenericType)
      {
        var def = iface.GetGenericTypeDefinition();
        var arg = iface.GetGenericArguments()[0];

        if (def == typeof(IStartWith<>))
        {
          var handler = methods.First(m =>
              m.Name == "HandleAsync" &&
              m.GetParameters().Length > 0 &&
              m.GetParameters()[0].ParameterType == arg);

          registration.StartHandlers[arg] = handler;
        }
        else if (def == typeof(IHandle<>))
        {
          var handler = methods.First(m =>
              m.Name == "HandleAsync" &&
              m.GetParameters().Length > 0 &&
              m.GetParameters()[0].ParameterType == arg);

          registration.StepHandlers[arg] = handler;
        }
        else if (def == typeof(ICompensateWith<>))
        {
          var handler = methods.First(m =>
              m.Name == "HandleAsync" &&
              m.GetParameters().Length > 0 &&
              m.GetParameters()[0].ParameterType == arg);

          registration.CompensationHandlers[arg] = handler;
        }
      }
    }

    return registration;
  }

  public bool CanStartWith(Type messageType) => StartHandlers.ContainsKey(messageType);
  public bool CanHandle(Type messageType) => StepHandlers.ContainsKey(messageType);
  public bool CanCompensate(Type messageType) => CompensationHandlers.ContainsKey(messageType);

  /// <summary>
  /// All message types handled by this saga across start, step, and compensation stages.
  /// </summary>
  public IEnumerable<Type> AllMessageTypes =>
      StartHandlers.Keys
          .Concat(StepHandlers.Keys)
          .Concat(CompensationHandlers.Keys)
          .Distinct();
}
