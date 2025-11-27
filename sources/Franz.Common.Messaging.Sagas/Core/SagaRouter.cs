#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

  public void RegisterSaga(Type sagaType)
  {
    var registration = SagaRegistration.FromType(sagaType);
    _registrations.TryAdd(sagaType, registration);
  }

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
}
