using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
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
using Franz.Common.Mediator.Validation;
using Franz.Common.Mediator.Validation.Events;
using Franz.Common.Mediator.Validation.Events.Preprocessing;
using Franz.Common.Mediator.Validation.Events.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Franz.Common.Mediator.Extensions
{
  public static class MediatorServiceCollectionExtensions
  {
    /// <summary>
    /// Registers Franz Mediator core (dispatcher + handlers).
    /// Pipelines are opt-in via the AddFranzXxxPipeline extensions.
    /// </summary>
    public static IServiceCollection AddFranzMediator(
        this IServiceCollection services,
        Assembly[] assemblies,
        Action<FranzMediatorOptions>? configure = null)
    {
      // Configure options (can be overridden by caller)
      var options = new FranzMediatorOptions();
      configure?.Invoke(options);

      services.AddSingleton(options);

      // Dispatcher
      services.AddScoped<IDispatcher, FranzDispatcher>();
      services.AddScoped<IEventDispatcher, EventDispatcher>();

      // -------------------- HANDLERS --------------------
      services.Scan(scan => scan
          .FromAssemblies(assemblies)
          .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
              .AsImplementedInterfaces().WithScopedLifetime()
          .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
              .AsImplementedInterfaces().WithScopedLifetime()
          .AddClasses(c => c.AssignableTo(typeof(INotificationHandler<>)))
              .AsImplementedInterfaces().WithScopedLifetime()
          .AddClasses(c => c.AssignableTo(typeof(IStreamQueryHandler<,>)))
              .AsImplementedInterfaces().WithScopedLifetime()
          .AddClasses(c => c.AssignableTo(typeof(IEventHandler<>)))
              .AsImplementedInterfaces().WithScopedLifetime()
      );

      // Observers (optional, but default console observer can be enabled)
      if (options.EnableDefaultConsoleObserver)
      {
        services.AddSingleton<IMediatorObserver, ConsoleMediatorObserver>();
      }

      return services;
    }

    // -------------------- PIPELINE EXTENSIONS --------------------

    public static IServiceCollection AddFranzLoggingPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(LoggingPipeline<,>));
      services.AddScoped(typeof(INotificationPipeline<>), typeof(NotificationLoggingPipeline<>));

      services.AddScoped(typeof(IPreProcessor<>), typeof(LoggingPreProcessor<>));
      services.AddScoped(typeof(IPostProcessor<,>), typeof(LoggingPostProcessor<,>));
      return services;
    }

    public static IServiceCollection AddFranzValidationPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(ValidationPipeline<,>));
      services.AddScoped(typeof(INotificationPipeline<>), typeof(NotificationValidationPipeline<>));

      services.AddScoped(typeof(IPreProcessor<>), typeof(AuditPreProcessor<>));
      return services;
    }

   
     public static IServiceCollection AddFranzBulkheadPipeline(this IServiceCollection services)
     {
      services.AddScoped(typeof(IPipeline<,>), typeof(BulkheadPipeline<,>));
      return services;
     }
    public static IServiceCollection AddFranzCircuitBreakerPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(CircuitBreakerPipeline<,>));
      return services;
    }
    public static IServiceCollection AddFranzRetryPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(RetryPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzTimeoutPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(TimeoutPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzEventValidationPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IEventPipeline<>), typeof(EventValidationPipeline<>))
                
               //Eventpreprocessors
              .AddScoped(typeof(IEventPreProcessor<>), typeof(EventAuditPreProcessor<>))
              .AddScoped(typeof(IEventPreProcessor<>), typeof(SerilogEventAuditPreProcessor<>))
              .AddScoped(typeof(IEventPreProcessor<>), typeof(SerilogEventLoggingPreProcessor<>))
              
              //MainLogginPipeline
              .AddScoped(typeof(IEventPipeline<>), typeof(SerilogEventLoggingPipeline<>))
              
              //eventpostprocessors
              .AddScoped(typeof(IEventPostProcessor<>), typeof(SerilogEventLoggingPostProcessor<>))
              .AddScoped(typeof(IEventPostProcessor<>), typeof(SerilogEventAuditPostProcessor<>))
              .AddScoped(typeof(IEventPostProcessor<>), typeof(EventAuditPostProcessor<>));

      return services;
    }

    public static IServiceCollection AddFranzTransactionPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(TransactionPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzSerilogLoggingPipeline(this IServiceCollection services)
    {
      // Core Serilog logging pipeline
      services.AddScoped(typeof(IPipeline<,>), typeof(SerilogLoggingPipeline<,>));
      services.AddScoped(typeof(INotificationPipeline<>), typeof(NotificationLoggingPipeline<>));

      // Pre/Post with Serilog context enrichment
      services.AddScoped(typeof(IPreProcessor<>), typeof(SerilogLoggingPreProcessor<>));
      services.AddScoped(typeof(IPostProcessor<,>), typeof(SerilogLoggingPostProcessor<,>));

      return services;
    }

    public static IServiceCollection AddFranzSerilogAuditPipeline(this IServiceCollection services)
    {
      // Validation pipeline itself
      services.AddScoped(typeof(IPipeline<,>), typeof(ValidationPipeline<,>));
      services.AddScoped(typeof(INotificationPipeline<>), typeof(NotificationValidationPipeline<>));

      // Pre/Post with Serilog context enrichment
      services.AddScoped(typeof(IPreProcessor<>), typeof(SerilogAuditPreProcessor<>));
      services.AddScoped(typeof(IPostProcessor<,>), typeof(SerilogAuditPostProcessor<,>));

      return services;
    }
        /// <summary>
      /// Registers Franz Mediator with a sensible default setup:
      /// - Scans the entry assembly for handlers
      /// - Enables logging, validation, audit pipelines
      /// - Adds default console observer if configured
      /// </summary>
      public static IServiceCollection AddFranzMediatorDefault(
          this IServiceCollection services,
          Action<FranzMediatorOptions>? configure = null)
      {
        var entryAssembly = Assembly.GetEntryAssembly()
                            ?? Assembly.GetCallingAssembly();

        // Core mediator (handlers + dispatcher)
        services.AddFranzMediator(new[] { entryAssembly }, configure);

        // Default pipelines: logging + validation + serilog audit
        services.AddFranzLoggingPipeline();
        services.AddFranzValidationPipeline();
        services.AddFranzSerilogLoggingPipeline();
        services.AddFranzSerilogAuditPipeline();


        // Transaction support
        services.AddFranzTransactionPipeline();

        return services;
      }
    }

    
}
