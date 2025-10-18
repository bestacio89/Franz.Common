using Franz.Common.Business.Events;
using Franz.Common.Integration.Tests.Mediator.Commands;
using Franz.Common.Integration.Tests.Mediator.Commands.Handlers.Events;
using Franz.Common.Integration.Tests.Mediator.Domain;
using Franz.Common.Integration.Tests.Mediator.Domain.Events;
using Franz.Common.Integration.Tests.Mediator.Integration;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Franz.Common.Integration.Tests.Mediator.Testing;

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
              IAggregateRootRepository<OrderAggregate, IDomainEvent>,
              InMemoryOrderRepository>();

          services.AddScoped<InMemoryProcessedEventSink>();
          services.AddScoped<IProcessedEventSink>(sp =>
              sp.GetRequiredService<InMemoryProcessedEventSink>());
        })
        .Build();
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
