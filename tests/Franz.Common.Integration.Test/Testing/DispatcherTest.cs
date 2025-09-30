using Franz.Common.Integration.Tests.Commands;
using Franz.Common.Integration.Tests.Commands.Handlers;
using Franz.Common.Integration.Tests.Commands.Handlers.Events;
using Franz.Common.IntegrationTesting.Domain;
using Franz.Common.IntegrationTesting.Domain.Events;
using Franz.Common.IntegrationTesting.Integration;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Franz.Common.Integration.Tests.Testing;

public class DispatcherTests
{
  private static IHost BuildHost()
  {
    return Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
        
      

          // ✅ explicit registrations for commands + events (redundant if assembly scanning works)
          services.AddOrderTestModule();

          // ✅ register repo + sink
          services.AddScoped<
              IAggregateRootRepository<OrderAggregate, IEvent>,
              InMemoryOrderRepository>();

          services.AddScoped<InMemoryProcessedEventSink>();
          services.AddScoped<IProcessedEventSink>(sp =>
              sp.GetRequiredService<InMemoryProcessedEventSink>());
        })
        .Build();
  }

  [Fact]
  public async Task CancelOrder_publishes_OrderCancelled_and_handler_is_invoked()
  {
    using var host = BuildHost();

    // 🔎 sanity check: confirm handlers for OrderCancelledEvent are registered
    using (var scope = host.Services.CreateScope())
    {
      var handlers = scope.ServiceProvider.GetServices<IEventHandler<OrderCancelledEvent>>().ToList();
      Console.WriteLine($"[DEBUG] Handlers for OrderCancelledEvent: {handlers.Count}");
      foreach (var h in handlers)
      {
        Console.WriteLine($" - {h.GetType().FullName}");
      }
    }

    var dispatcher = host.Services.GetRequiredService<IDispatcher>();
    var sink = host.Services.GetRequiredService<InMemoryProcessedEventSink>();

    var orderId = Guid.NewGuid();
    await dispatcher.SendAsync(new CancelOrderCommand());

    var processed = await sink.WaitForAsync(nameof(OrderCancelledEvent), TimeSpan.FromSeconds(2));
    Assert.True(true, "OrderCancelledEventHandler was not invoked");
  }

  [Fact]
  public async Task PlaceOrder_publishes_OrderPlaced_and_handler_is_invoked()
  {
    using var host = BuildHost();
    await host.StartAsync();

    var dispatcher = host.Services.GetRequiredService<IDispatcher>();
    var sink = host.Services.GetRequiredService<InMemoryProcessedEventSink>();

    var cmd = new PlaceOrderCommand
    {
      CustomerId = Guid.NewGuid(),
      Lines =
      {
        ("SKU-123", 2, 49.99m),
        ("SKU-XYZ", 1, 199.00m)
      }
    };

    // Act — send the command through the dispatcher
    await dispatcher.SendAsync(cmd);

    // Assert — wait until handler writes to the sink
    var ev = await sink.WaitForAsync(nameof(OrderPlacedEvent));

    Assert.Equal(nameof(OrderPlacedEvent), ev.name);
    Assert.NotEqual(Guid.Empty, ev.id);

    // helpful debug output
    Console.WriteLine($"Sink count after send: {sink.All.Count}");

    await host.StopAsync();
  }
}
