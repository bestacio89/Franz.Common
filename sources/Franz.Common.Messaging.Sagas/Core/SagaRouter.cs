#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Resolves which saga type and saga instance should process incoming messages.
/// </summary>
public sealed class SagaRouter
{
  private readonly ConcurrentDictionary<Type, SagaRegistration> _registrations = new();
  private readonly IServiceProvider _services;

  public SagaRouter(IServiceProvider services)
  {
    _services = services;
  }

  // --------------------------------------------------------------------
  // 1) Explicit saga registration
  // --------------------------------------------------------------------
  public void RegisterSaga(Type sagaType)
  {
    var reg = SagaRegistration.FromType(sagaType);
    _registrations.TryAdd(sagaType, reg);
  }

  public void RegisterSaga<TSaga>() =>
      RegisterSaga(typeof(TSaga));

  // --------------------------------------------------------------------
  // 2) Assembly scanning
  // --------------------------------------------------------------------
  public void RegisterSagasFromAssembly(Assembly assembly)
  {
    var sagaTypes = assembly
      .GetTypes()
      .Where(IsSagaType);

    foreach (var sagaType in sagaTypes)
      RegisterSaga(sagaType);
  }

  public void RegisterSagasFromAssemblyContaining<T>() =>
      RegisterSagasFromAssembly(typeof(T).Assembly);

  // --------------------------------------------------------------------
  // Helpers
  // --------------------------------------------------------------------
  public IEnumerable<SagaRegistration> GetRegistrations() => _registrations.Values;

  /// <summary>
  /// Returns saga registrations capable of handling the message.
  /// There may be multiple sagas that react to the same message.
  /// </summary>
  public IEnumerable<SagaRegistration> ResolveRegistrationsForMessage(Type messageType)
  {
    foreach (var reg in _registrations.Values)
    {
      if (reg.CanStartWith(messageType) ||
          reg.CanHandle(messageType) ||
          reg.CanCompensate(messageType))
      {
        yield return reg;
      }
    }
  }

  // ---------------------------------------------------------------
  // Static helper for identifying saga types
  // ---------------------------------------------------------------
  private static bool IsSagaType(Type type)
  {
    if (type.IsAbstract || type.IsInterface)
      return false;

    return type
      .GetInterfaces()
      .Any(i =>
          i.IsGenericType &&
          i.GetGenericTypeDefinition() == typeof(ISaga<>));
  }
}
