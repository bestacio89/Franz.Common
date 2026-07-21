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
using System;
using System.Reflection;

namespace Franz.Common.Mediator.Extensions;

/// <summary>
/// Franz Mediator V2 registration extensions.
/// Uses zero-reflection, source-generated handler registration for Native AOT readiness.
/// </summary>
public static class MediatorServiceCollectionExtensionsV2
{
  /// <summary>
  /// Registers core Franz Mediator infrastructure and wires handlers using compile-time source generation.
  /// </summary>
  public static IServiceCollection AddFranzMediatorV2(
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
      services.TryAddSingleton<ConsoleObserverOptions>();
      services.TryAddEnumerable(
          ServiceDescriptor.Singleton<
              IMediatorObserver,
              ConsoleMediatorObserver>());
    }

    // Direct invocation of source-generated handler provider (Zero Reflection / Native AOT)
    services.AddFranzGeneratedHandlerRegistration();

    return services;
  }

  /// <summary>
  /// Standalone extension to register source-generated handlers via provider.
  /// </summary>

  public static IServiceCollection AddFranzGeneratedHandlerRegistration(
         this IServiceCollection services,
         Assembly? targetAssembly = null)
  {
    ArgumentNullException.ThrowIfNull(services);

        // Explicitly fallback to Assembly.GetCallingAssembly() or Assembly.GetEntryAssembly()
        // when called directly from the consumer/test target assembly
        var assembly = targetAssembly
            ?? Assembly.GetCallingAssembly()
            ?? Assembly.GetEntryAssembly();

        var provider = new GeneratedHandlerRegistrationProvider(assembly);
    provider.Register(services);

    return services;
  }

  /// <summary>
  /// Registers Franz Mediator V2 with the default recommended enterprise pipeline setup.
  /// </summary>
  public static IServiceCollection AddFranzMediatorV2Default(
      this IServiceCollection services,
      Action<FranzMediatorOptions>? configure = null)
  {
    services.AddFranzMediatorV2(configure);

    services.AddFranzLoggingPipelineV2();
    services.AddFranzValidationPipelineV2();
    services.AddFranzSerilogLoggingPipelineV2();
    services.AddFranzSerilogAuditPipelineV2();
    services.AddFranzTransactionPipelineV2();

    return services;
  }

  // -------------------- PIPELINES --------------------

  public static IServiceCollection AddFranzLoggingPipelineV2(
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

  public static IServiceCollection AddFranzValidationPipelineV2(
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

  public static IServiceCollection AddFranzTransactionPipelineV2(
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

  public static IServiceCollection AddFranzSerilogLoggingPipelineV2(
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

  public static IServiceCollection AddFranzSerilogAuditPipelineV2(
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

  public static IServiceCollection AddFranzRetryPipelineV2(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(RetryPipeline<,>)));

    return services;
  }

  public static IServiceCollection AddFranzTimeoutPipelineV2(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(TimeoutPipeline<,>)));

    return services;
  }

  public static IServiceCollection AddFranzCircuitBreakerPipelineV2(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(CircuitBreakerPipeline<,>)));

    return services;
  }

  public static IServiceCollection AddFranzBulkheadPipelineV2(
      this IServiceCollection services)
  {
    services.TryAddEnumerable(
        ServiceDescriptor.Scoped(
            typeof(IPipeline<,>),
            typeof(BulkheadPipeline<,>)));

    return services;
  }

  public static IServiceCollection AddFranzEventValidationPipelineV2(
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
}