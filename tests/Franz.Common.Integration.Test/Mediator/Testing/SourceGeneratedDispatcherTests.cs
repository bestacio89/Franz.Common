using Franz.Common.Business.Events;
using Franz.Common.EntityFramework;
using Franz.Common.Integration.Tests.Mediator.Commands;
using Franz.Common.Integration.Tests.Mediator.Commands.Handlers.Events;
using Franz.Common.Integration.Tests.Mediator.Domain;
using Franz.Common.Integration.Tests.Mediator.Domain.Events;
using Franz.Common.Integration.Tests.Mediator.Integration;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Xunit;

namespace Franz.Common.Integration.Tests.Mediator.Testing;

public class SourceGeneratedDispatcherTests
{
  private static IHost BuildHost()
  {
    return Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
          // 🚀 Zero-Reflection Source Generated Registration
          // Registers FranzMediator V2 infrastructure and executes compile-time generated handler mappings.
          services.AddFranzMediatorV2Default();

          // Ensure handlers declared in the test assembly are discovered when source-generation is not emitted
          services.AddFranzGeneratedHandlerRegistration(Assembly.GetExecutingAssembly());

          // ✅ Register required repositories and sink infrastructure
          services.AddScoped<
              IAggregateRootRepository<OrderAggregate, IDomainEvent, Guid>,
              InMemoryOrderRepository>();

          services.AddScoped<InMemoryProcessedEventSink>();
          services.AddScoped<IProcessedEventSink>(sp =>
              sp.GetRequiredService<InMemoryProcessedEventSink>());
          services.AddScoped<IUnitOfWork, TestUnitOfWork>();

        })
        .Build();
  }

  [Fact]
  public async Task PlaceOrder_publishes_OrderPlaced_and_handler_is_invoked_via_source_generation()
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

    // Act — send the command through the source-generated dispatcher pipeline
    await dispatcher.SendAsync(cmd);

    // Assert — wait until handler writes to the sink
    var ev = await sink.WaitForAsync(nameof(OrderPlacedEvent));

    Assert.Equal(nameof(OrderPlacedEvent), ev.name);
    Assert.NotEqual(Guid.Empty, ev.id);

    // Debug output
    Console.WriteLine($"Sink count after send (Source Generated): {sink.All.Count}");

    await host.StopAsync();
  }
}