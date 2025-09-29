using Franz.Common.Integration.Tests.Commands.Handlers.Events;
using Franz.Common.IntegrationTesting.Commands;
using Franz.Common.IntegrationTesting.Commands.Handlers.Events;
using Franz.Common.IntegrationTesting.Integration;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
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
          // ✅ mediator setup, scans all relevant assemblies
          services.AddFranzMediator(new[] { typeof(PlaceOrderHandler).Assembly,
                          typeof(OrderPlacedEventHandler).Assembly});

          // ✅ test domain handlers
          services.AddOrderTestModule();

          // ✅ register sink once and share it across IProcessedEventSink + test
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

    await dispatcher.SendAsync(cmd);
   
    // ✅ wait asynchronously until the handler writes to the sink
    var ev = await sink.WaitForAsync(nameof(OrderPlacedEvent));
    Console.WriteLine("Sink count after send: " + sink.All.Count);
    Assert.Equal(nameof(OrderPlacedEvent), ev.name);
    Assert.NotEqual(Guid.Empty, ev.id);

    await host.StopAsync();
  }
}
