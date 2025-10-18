using Franz.Common.Integration.Tests.Mediator.Commands.Handlers.Events;
using Franz.Common.Integration.Tests.Mediator.Domain.Events;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Integration.Tests.Mediator.Commands.Handlers;
public sealed class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
  private readonly IProcessedEventSink _sink;
  private readonly ILogger<OrderPlacedEventHandler> _logger;

  public OrderPlacedEventHandler(IProcessedEventSink sink, ILogger<OrderPlacedEventHandler> logger)
  {
    _sink = sink;
    _logger = logger;
  }

  public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("📦 OrderPlacedEvent handled for Aggregate {AggregateId}", @event.AggregateId);
    _logger.LogInformation($"Handler fired: {@event.AggregateId}");
    _sink.Add(nameof(OrderPlacedEvent), @event.AggregateId.GetValueOrDefault());

    return Task.CompletedTask;
  }
}