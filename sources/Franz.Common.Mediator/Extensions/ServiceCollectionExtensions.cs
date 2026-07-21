using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Observers;
using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Events.Logging;
using Franz.Common.Mediator.Pipelines.Events.PostProcessing;
using Franz.Common.Mediator.Pipelines.Events.Preprocessing;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Pipelines.Processors;
using Franz.Common.Mediator.Pipelines.Processors.Logging;
using Franz.Common.Mediator.Pipelines.Processors.Validation;
using Franz.Common.Mediator.Pipelines.Resilience;
using Franz.Common.Mediator.Pipelines.Transaction;
using Franz.Common.Mediator.Pipelines.Validation;
using Franz.Common.Mediator.Registration;
using Franz.Common.Mediator.Validation.Events;
using Franz.Common.Mediator.Validation.Events.Preprocessing;
using Franz.Common.Mediator.Validation.Events.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Franz.Common.Mediator.Extensions;

public static class MediatorServiceCollectionExtensions
{
  /// <summary>
  /// Registers Franz Mediator core infrastructure.
  /// Handler registration is provided separately through a registration provider.
  /// </summary>
  public static IServiceCollection AddFranzMediator(
      this IServiceCollection services,
      Action<FranzMediatorOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);

    var options = new FranzMediatorOptions();

    configure?.Invoke(options);

    services.TryAddSingleton(options);

    services.TryAddScoped<IDispatcher, FranzDispatcher>();

    if (options.EnableDefaultConsoleObserver)
    {
      services.TryAddEnumerable(
          ServiceDescriptor.Singleton<
              IMediatorObserver,
              ConsoleMediatorObserver>());
    }

    return services;
  }


  /// <summary>
  /// Registers Franz Mediator and discovers handlers from assemblies.
  /// Uses reflection registration provider.
  /// </summary>
  public static IServiceCollection AddFranzMediator(
      this IServiceCollection services,
      Assembly[] assemblies,
      Action<FranzMediatorOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(assemblies);

    services.AddFranzMediator(configure);

    var registrationProvider =
        new ReflectionHandlerRegistrationProvider(assemblies);

    registrationProvider.Register(services);

    return services;
  }


  // -------------------- DEFAULT SETUP --------------------

  /// <summary>
  /// Registers Franz Mediator using the assembly containing the marker type.
  /// Enables default enterprise pipelines.
  /// </summary>
  public static IServiceCollection AddFranzMediatorDefault<TAssemblyMarker>(
      this IServiceCollection services,
      Action<FranzMediatorOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddFranzMediator(
        new[]
        {
                typeof(TAssemblyMarker).Assembly
        },
        configure);

    services.AddFranzLoggingPipeline();
    services.AddFranzValidationPipeline();
    services.AddFranzSerilogLoggingPipeline();
    services.AddFranzSerilogAuditPipeline();
    services.AddFranzTransactionPipeline();

    return services;
  }


  // -------------------- PIPELINES --------------------


  public static IServiceCollection AddFranzLoggingPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(LoggingPipeline<,>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(INotificationPipeline<>),
            typeof(NotificationLoggingPipeline<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPreProcessor<>),
            typeof(LoggingPreProcessor<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPostProcessor<,>),
            typeof(LoggingPostProcessor<,>)));

    return services;
  }


  public static IServiceCollection AddFranzValidationPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(ValidationPipeline<,>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(INotificationPipeline<>),
            typeof(NotificationValidationPipeline<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPreProcessor<>),
            typeof(AuditPreProcessor<>)));

    return services;
  }


  public static IServiceCollection AddFranzBulkheadPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(BulkheadPipeline<,>)));

    return services;
  }


  public static IServiceCollection AddFranzCircuitBreakerPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(CircuitBreakerPipeline<,>)));

    return services;
  }


  public static IServiceCollection AddFranzRetryPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(RetryPipeline<,>)));

    return services;
  }


  public static IServiceCollection AddFranzTimeoutPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(TimeoutPipeline<,>)));

    return services;
  }


  public static IServiceCollection AddFranzEventValidationPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPipeline<>),
            typeof(EventValidationPipeline<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPipeline<>),
            typeof(SerilogEventLoggingPipeline<>)));


    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPreProcessor<>),
            typeof(EventAuditPreProcessor<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPreProcessor<>),
            typeof(SerilogEventAuditPreProcessor<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPreProcessor<>),
            typeof(SerilogEventLoggingPreProcessor<>)));


    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPostProcessor<>),
            typeof(SerilogEventLoggingPostProcessor<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPostProcessor<>),
            typeof(SerilogEventAuditPostProcessor<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IEventPostProcessor<>),
            typeof(EventAuditPostProcessor<>)));

    return services;
  }


  public static IServiceCollection AddFranzTransactionPipeline(
      this IServiceCollection services,
      Action<TransactionOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);

    var options = new TransactionOptions();

    configure?.Invoke(options);

    services.TryAddSingleton(options);

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(TransactionPipeline<,>)));

    return services;
  }


  public static IServiceCollection AddFranzSerilogLoggingPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(SerilogLoggingPipeline<,>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(INotificationPipeline<>),
            typeof(NotificationLoggingPipeline<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPreProcessor<>),
            typeof(SerilogLoggingPreProcessor<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPostProcessor<,>),
            typeof(SerilogLoggingPostProcessor<,>)));

    return services;
  }


  public static IServiceCollection AddFranzSerilogAuditPipeline(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(ValidationPipeline<,>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(INotificationPipeline<>),
            typeof(NotificationValidationPipeline<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPreProcessor<>),
            typeof(SerilogAuditPreProcessor<>)));

    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPostProcessor<,>),
            typeof(SerilogAuditPostProcessor<,>)));

    return services;
  }
}