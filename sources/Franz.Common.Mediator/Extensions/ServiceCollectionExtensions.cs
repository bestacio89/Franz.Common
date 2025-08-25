using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Pipelines;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Franz.Common.Mediator.Extensions
{
  public static class MediatorServiceCollectionExtensions
  {
    /// <summary>
    /// Registers Franz mediator core + custom pipelines.
    /// </summary>
    public static IServiceCollection AddFranzMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
      // Core dispatcher
      services.AddScoped<IDispatcher, Dispatcher>();

      // Scan assemblies for handlers (commands + queries)
      services.Scan(scan => scan
          .FromAssemblies(assemblies)
          .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
              .AsImplementedInterfaces()
              .WithScopedLifetime()
          .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
              .AsImplementedInterfaces()
              .WithScopedLifetime()
      );

      // Register Franz pipelines
      services.AddScoped(typeof(IPipeline<,>), typeof(LoggingPipeline<,>));
      services.AddScoped(typeof(IPipeline<,>), typeof(ValidationPipeline<,>));

      return services;
    }

    /// <summary>
    /// Adds MediatR-compatible pipelines (optional).
    /// Useful if mixing Franz + MediatR.
    /// </summary>
    public static IServiceCollection AddMediatRCompatiblePipelines(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
      // This lets you reuse existing MediatR behaviors
      services.AddMediatR(cfg =>
      {
        cfg.RegisterServicesFromAssemblies(assemblies);
      });

      return services;
    }
  }
}
