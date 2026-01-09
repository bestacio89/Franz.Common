#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Messaging.Sagas.Configuration;

public sealed class FranzSagaBuilder
{
  private readonly IServiceCollection _services;
  private readonly List<Type> _sagaTypes = new();

  public FranzSagaBuilder(IServiceCollection services)
  {
    _services = services;
    _services.AddSingleton(this);
  }

  // ----------------------------------------------------
  // Explicit registration
  // ----------------------------------------------------
  public FranzSagaBuilder AddSaga<TSaga>()
  {
    return AddSaga(typeof(TSaga));
  }

  public FranzSagaBuilder AddSaga(Type sagaType)
  {
    if (!IsSagaType(sagaType))
      throw new InvalidOperationException($"{sagaType.Name} is not a valid saga. It must implement ISaga<TState>");

    _sagaTypes.Add(sagaType);
    _services.AddTransient(sagaType);
    return this;
  }

  // ----------------------------------------------------
  // Assembly scanning
  // ----------------------------------------------------
  public FranzSagaBuilder AddSagaAssembly(Assembly assembly)
  {
    var sagas = assembly
      .GetTypes()
      .Where(IsSagaType);

    foreach (var saga in sagas)
      AddSaga(saga);

    return this;
  }

  public FranzSagaBuilder AddSagasFromAssemblyContaining<T>()
  {
    return AddSagaAssembly(typeof(T).Assembly);
  }

  // ----------------------------------------------------
  // Audit sink
  // ----------------------------------------------------
  public FranzSagaBuilder AddAuditSink<TSink>()
      where TSink : class, ISagaAuditSink
  {
    _services.TryAddEnumerable(
      ServiceDescriptor.Singleton<ISagaAuditSink, TSink>()
    );
    return this;
  }

  // ----------------------------------------------------
  // Internal Build for DI
  // ----------------------------------------------------
  internal void Build(IServiceProvider provider, bool validate)
  {
    var router = provider.GetRequiredService<SagaRouter>();
    RegisterIntoRouter(router, validate);
  }

  // ----------------------------------------------------
  // Manual registration for Tests
  // ----------------------------------------------------
  public void RegisterIntoRouter(SagaRouter router, bool validate)
  {
    foreach (var type in _sagaTypes)
    {
      if (validate)
      {
        var reg = SagaRegistration.FromType(type);
        SagaMappingValidator.ValidateRegistration(reg);
      }

      router.RegisterSaga(type);
    }
  }

  // ----------------------------------------------------
  // Helper: detect saga types
  // ----------------------------------------------------
  private static bool IsSagaType(Type type)
  {
    if (type.IsAbstract || type.IsInterface)
      return false;

    return type
      .GetInterfaces()
      .Any(i =>
          i.IsGenericType &&
          i.GetGenericTypeDefinition() == typeof(ISaga<>)
      );
  }
}
