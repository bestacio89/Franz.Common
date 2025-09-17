using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Observers;
using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Caching;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Pipelines.Processors;
using Franz.Common.Mediator.Pipelines.Processors.Logging;
using Franz.Common.Mediator.Pipelines.Processors.Validation;
using Franz.Common.Mediator.Pipelines.Resilience;
using Franz.Common.Mediator.Pipelines.Transaction;
using Franz.Common.Mediator.Pipelines.Validation;
using Franz.Common.Mediator.Validation;
using Microsoft.Extensions.DependencyInjection;
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

      services.AddScoped(typeof(IPreProcessor<>), typeof(ValidationPreProcessor<>));
      return services;
    }

    public static IServiceCollection AddFranzResiliencePipelines(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(RetryPipeline<,>));
      services.AddScoped(typeof(IPipeline<,>), typeof(TimeoutPipeline<,>));
      services.AddScoped(typeof(IPipeline<,>), typeof(CircuitBreakerPipeline<,>));
      services.AddScoped(typeof(IPipeline<,>), typeof(BulkheadPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzTransactionPipeline(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipeline<,>), typeof(TransactionPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzCachingPipeline(this IServiceCollection services, Action<CachingOptions>? configure = null)
    {
      var options = new CachingOptions();
      configure?.Invoke(options);
      services.AddSingleton(options);

      services.AddScoped(typeof(IPipeline<,>), typeof(CachingPipeline<,>));
      return services;
    }
  }
}
