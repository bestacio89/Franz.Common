#nullable enable

using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;

namespace Franz.Common.Messaging.Sagas.Configuration;

/// <summary>
/// Fluent builder used to configure sagas and their registrations.
/// </summary>
public sealed class FranzSagaBuilder
{
  private readonly IServiceCollection _services;
  private readonly List<Type> _sagaTypes = new();

  public FranzSagaBuilder(IServiceCollection services)
  {
    _services = services;
    _services.AddSingleton<FranzSagaBuilder>(this); // Needed for BuildFranzSagas()
  }

  /// <summary>
  /// Registers a saga type implementing ISaga<TState>.
  /// </summary>
  public FranzSagaBuilder AddSaga<TSaga>()
  {
    var sagaType = typeof(TSaga);
    _sagaTypes.Add(sagaType);

    _services.AddTransient(sagaType);
    return this;
  }

  /// <summary>
  /// Registers a custom audit sink.
  /// </summary>
  public FranzSagaBuilder AddAuditSink<TSink>()
      where TSink : class, ISagaAuditSink
  {
    _services.TryAddEnumerable(ServiceDescriptor.Singleton<ISagaAuditSink, TSink>());
    return this;
  }

  /// <summary>
  /// Internal usage: finalizes saga registrations.
  /// Called by BuildFranzSagas(IServiceProvider).
  /// </summary>
  internal void Build(IServiceProvider provider, bool validateMappings)
  {
    var router = provider.GetRequiredService<SagaRouter>();

    foreach (var sagaType in _sagaTypes)
    {
      // Build registration only to validate mapping
      if (validateMappings)
      {
        var registration = SagaRegistration.FromType(sagaType);
        SagaMappingValidator.ValidateRegistration(registration);
      }

      // Register through the router
      router.RegisterSaga(sagaType);
    }
  }
}
