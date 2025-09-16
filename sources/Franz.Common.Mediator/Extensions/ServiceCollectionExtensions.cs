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
    /// Registers Franz Mediator with dispatcher, handlers, pipelines, processors, and observers.
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

      // -------------------- PIPELINES --------------------
      // Core
      services.AddScoped(typeof(IPipeline<,>), typeof(LoggingPipeline<,>));
      services.AddScoped(typeof(IPipeline<,>), typeof(ValidationPipeline<,>));

      // Resilience
      services.AddScoped(typeof(IPipeline<,>), sp => new RetryPipeline<object, object>(options.Retry));
      services.AddScoped(typeof(IPipeline<,>), sp => new TimeoutPipeline<object, object>(options.Timeout));
      services.AddScoped(typeof(IPipeline<,>), sp => new CircuitBreakerPipeline<object, object>(options.CircuitBreaker));
      services.AddScoped(typeof(IPipeline<,>), sp => new BulkheadPipeline<object, object>(options.Bulkhead));

      // Transaction
      services.AddScoped(typeof(IPipeline<,>), sp =>
      {
        var uow = sp.GetRequiredService<IUnitOfWork>();
        return new TransactionPipeline<object, object>(uow, options.Transaction);
      });

      // Caching
      services.AddScoped(typeof(IPipeline<,>), sp =>
      {
        var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        return new CachingPipeline<object, object>(cache, options.Caching);
      });

      // Notification pipelines
      services.AddScoped(typeof(INotificationPipeline<>), typeof(NotificationLoggingPipeline<>));
      services.AddScoped(typeof(INotificationPipeline<>), typeof(NotificationValidationPipeline<>));

      // -------------------- PROCESSORS --------------------
      services.AddScoped(typeof(IPreProcessor<>), typeof(LoggingPreProcessor<>));
      services.AddScoped(typeof(IPostProcessor<,>), typeof(LoggingPostProcessor<,>));
      services.AddScoped(typeof(IPreProcessor<>), typeof(ValidationPreProcessor<>));
      services.AddScoped(typeof(IPostProcessor<,>), typeof(AuditPostProcessor<,>));

      // -------------------- OBSERVERS --------------------
      if (options.EnableDefaultConsoleObserver)
      {
        services.AddSingleton<IMediatorObserver, ConsoleMediatorObserver>();
      }

      return services;
    }
  }
}
