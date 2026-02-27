using Franz.Common.Business.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.AggregateRootTests;

internal sealed record TestEvent(string Name) : IDomainEvent
{
  public Guid EventId { get; } = Guid.CreateVersion7();
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
  public Guid? CorrelationId => null;
  public Guid? AggregateId => null;
  public string AggregateType => "Test";
  public string EventType => nameof(TestEvent);
}