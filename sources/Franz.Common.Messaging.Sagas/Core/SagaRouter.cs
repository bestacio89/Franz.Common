#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Franz.Common.Messaging;
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
  /// Resolves a message type only when it is part of a registered saga contract.
  /// Assembly-qualified names are preferred; unqualified names must be unambiguous.
  /// </summary>
  public bool TryResolveMessageType(string typeName, out Type? messageType)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

    var registeredTypes = _registrations.Values
        .SelectMany(registration => registration.AllMessageTypes)
        .Distinct()
        .ToArray();

    var assemblyQualifiedMatch = registeredTypes.SingleOrDefault(type =>
        string.Equals(type.AssemblyQualifiedName, typeName, StringComparison.Ordinal));

    if (assemblyQualifiedMatch is not null)
    {
      messageType = assemblyQualifiedMatch;
      return true;
    }

    // HeaderNamer emits "FullName, AssemblySimpleName". This is deliberately
    // matched against registered saga contracts rather than passed to Type.GetType.
    var canonicalMatches = registeredTypes
        .Where(type => string.Equals(
            HeaderNamer.GetEventClassName(type),
            typeName,
            StringComparison.Ordinal))
        .Take(2)
        .ToArray();

    if (canonicalMatches.Length == 1)
    {
      messageType = canonicalMatches[0];
      return true;
    }

    var fullNameMatches = registeredTypes
        .Where(type => string.Equals(type.FullName, typeName, StringComparison.Ordinal))
        .Take(2)
        .ToArray();

    if (fullNameMatches.Length == 1)
    {
      messageType = fullNameMatches[0];
      return true;
    }

    messageType = null;
    return false;
  }

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
