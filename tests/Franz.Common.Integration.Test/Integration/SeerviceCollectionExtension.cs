using Franz.Common.Integration.Tests.Commands.Handlers.Events;
using Franz.Common.IntegrationTesting.Commands;
using Franz.Common.IntegrationTesting.Commands.Handlers;
using Franz.Common.IntegrationTesting.Domain.Events;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using global::Franz.Common.IntegrationTesting.Commands.Handlers.Events;
using global::Franz.Common.Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Mediator.Extensions; 

namespace Franz.Common.IntegrationTesting.Integration;
// tests/Franz.IntegrationTests/TestBootstrap/ServiceCollectionExtensions.cs




public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddOrderTestModule(this IServiceCollection services)
  {
    // Handlers
    services.AddScoped<ICommandHandler<PlaceOrderCommand, Unit>, PlaceOrderHandler>();
    services.AddScoped<ICommandHandler<CancelOrderCommand, Unit>, CancelOrderHandler>();
    services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedEventHandler>();
    services.AddScoped<IEventHandler<OrderCancelledEvent>, OrderCancelledEventHandler>();

    // ✅ Sink must be singleton to be visible to both test + handlers
    services.AddSingleton<InMemoryProcessedEventSink>();
    services.AddSingleton<IProcessedEventSink>(sp => sp.GetRequiredService<InMemoryProcessedEventSink>());
    services.AddFranzMediator(new[] { typeof(PlaceOrderHandler).Assembly,
                          typeof(OrderPlacedEventHandler).Assembly});
    services.AddFranzEventValidationPipeline();

    return services;
  }

}
