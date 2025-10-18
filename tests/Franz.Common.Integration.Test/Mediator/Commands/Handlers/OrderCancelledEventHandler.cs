using Franz.Common.Integration.Tests.Mediator.Commands.Handlers.Events;
using Franz.Common.IntegrationTesting.Domain.Events;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Integration.Tests.Mediator.Commands.Handlers;
public sealed class OrderCancelledEventHandler : IEventHandler<OrderCancelledEvent>
{
  private readonly IProcessedEventSink _sink;
  private readonly ILogger<OrderCancelledEventHandler> _logger;

  public OrderCancelledEventHandler(IProcessedEventSink sink, ILogger<OrderCancelledEventHandler> logger)
  {
    _sink = sink;
    _logger = logger;
  }

  public Task HandleAsync(OrderCancelledEvent notification, CancellationToken ct = default)
  {
    _logger.LogInformation("Handled OrderCancelled for {AggregateId}", notification.AggregateId);
    _sink.Add(nameof(OrderCancelledEvent), notification.AggregateId ?? Guid.Empty);
    return Task.CompletedTask;
  }
}
