#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Handlers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Franz.Common.Messaging.Sagas.Configuration;

public sealed class FranzSagaBuilder
{
  private readonly IServiceCollection _services;
  private readonly List<Type> _sagaTypes = new();
  private readonly List<Func<Func<Task>, Task>> _pipelineMiddlewares = new();
  private bool _built;

  public FranzSagaBuilder(IServiceCollection services)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
  }

  public FranzSagaBuilder AddSaga<TSaga>() => AddSaga(typeof(TSaga));

  public FranzSagaBuilder AddSaga(Type sagaType)
  {
    EnsureNotBuilt();
    ArgumentNullException.ThrowIfNull(sagaType);

    if (!IsSagaType(sagaType))
      throw new InvalidOperationException(
          $"{sagaType.Name} is not a valid saga. It must implement ISaga<TState>.");

    if (_sagaTypes.Contains(sagaType))
      throw new InvalidOperationException($"Saga '{sagaType.FullName}' has already been registered.");

    var registration = SagaRegistration.FromType(sagaType);
    foreach (var messageType in registration.AllMessageTypes)
    {
      if (!typeof(IIntegrationEvent).IsAssignableFrom(messageType))
        throw new InvalidOperationException(
            $"Saga message type '{messageType.FullName}' must implement IIntegrationEvent.");

      var notificationType = typeof(INotificationHandler<>).MakeGenericType(messageType);
      var handlerType = typeof(SagaIntegrationEventNotificationHandler<>).MakeGenericType(messageType);
      _services.TryAddEnumerable(ServiceDescriptor.Scoped(notificationType, handlerType));
    }

    _sagaTypes.Add(sagaType);
    _services.TryAdd(ServiceDescriptor.Transient(sagaType, sagaType));
    return this;
  }

  public FranzSagaBuilder AddSagaAssembly(Assembly assembly)
  {
    EnsureNotBuilt();
    ArgumentNullException.ThrowIfNull(assembly);

    foreach (var saga in assembly.GetTypes().Where(IsSagaType))
      AddSaga(saga);

    return this;
  }

  public FranzSagaBuilder AddSagasFromAssemblyContaining<T>()
      => AddSagaAssembly(typeof(T).Assembly);

  public FranzSagaBuilder AddAuditSink<TSink>()
      where TSink : class, ISagaAuditSink
  {
    EnsureNotBuilt();
    _services.TryAddEnumerable(ServiceDescriptor.Singleton<ISagaAuditSink, TSink>());
    return this;
  }

  public FranzSagaBuilder UsePipeline(Func<Func<Task>, Task> middleware)
  {
    EnsureNotBuilt();
    ArgumentNullException.ThrowIfNull(middleware);
    _pipelineMiddlewares.Add(middleware);
    return this;
  }

  internal void Build(IServiceProvider provider, bool validate)
  {
    if (_built)
      return;

    if (!_services.Any(descriptor => descriptor.ServiceType == typeof(ISagaRepository)))
      throw new InvalidOperationException(
          "No ISagaRepository is registered. Configure a supported saga persistence adapter.");

    if (!_services.Any(descriptor => descriptor.ServiceType == typeof(IMessagingPublisher)))
      throw new InvalidOperationException(
          "No IMessagingPublisher is registered. Saga orchestration requires a messaging publisher.");

    var pipeline = provider.GetRequiredService<SagaExecutionPipeline>();
    foreach (var middleware in _pipelineMiddlewares)
      pipeline.Use(middleware);
    pipeline.Freeze();

    var router = provider.GetRequiredService<SagaRouter>();
    RegisterIntoRouter(router, validate);
    _built = true;
  }

  public void RegisterIntoRouter(SagaRouter router, bool validate)
  {
    ArgumentNullException.ThrowIfNull(router);

    foreach (var type in _sagaTypes)
    {
      var registration = SagaRegistration.FromType(type);
      SagaTypeValidator.ValidateSagaType(type);

      if (validate)
        SagaMappingValidator.ValidateRegistration(registration);

      router.RegisterSaga(type);
    }
  }

  private void EnsureNotBuilt()
  {
    if (_built)
      throw new InvalidOperationException("Saga registration cannot be changed after startup finalization.");
  }

  private static bool IsSagaType(Type type)
  {
    if (!type.IsClass || type.IsAbstract || type.IsInterface)
      return false;

    return type.GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISaga<>));
  }
}
