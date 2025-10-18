using Franz.Common.Integration.Tests.Mediator.Commands.Handlers;
using Franz.Common.Integration.Tests.Mediator.Commands.Handlers.Events;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Integration.Tests.Mediator.Integration;
// tests/Franz.IntegrationTests/TestBootstrap/ServiceCollectionExtensions.cs




public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddOrderTestModule(this IServiceCollection services)
  {
    // Handlers
   // services.Scan(scan => scan
   //.FromAssemblies(typeof(OrderPlacedEventHandler).Assembly)
   //.AddClasses(c => c.AssignableTo(typeof(IEventHandler<>)))
   //.AsImplementedInterfaces()
   //.WithScopedLifetime());
   // services.Scan(scan => scan
   //.FromAssemblies(typeof(OrderCancelledEventHandler).Assembly)
   //.AddClasses(c => c.AssignableTo(typeof(IEventHandler<>)))
   //.AsImplementedInterfaces()
   //.WithScopedLifetime());



    // ✅ Sink must be singleton to be visible to both test + handlers
    services.AddSingleton<InMemoryProcessedEventSink>();
    services.AddSingleton<IProcessedEventSink>(sp => sp.GetRequiredService<InMemoryProcessedEventSink>());
    services.AddFranzMediator(new[] { typeof(PlaceOrderHandler).Assembly,
                          typeof(OrderPlacedEventHandler).Assembly});
    services.AddFranzEventValidationPipeline();


    return services;
  }

}
