using Franz.Common.Business.Events;

namespace Franz.Common.Business.Tests.Samples;

public class SomethingCreated : IDomainEvent
{
  public Guid EventId => throw new NotImplementedException();

  public DateTimeOffset OccurredOn => throw new NotImplementedException();

  public string? CorrelationId => throw new NotImplementedException();

  public Guid? AggregateId => throw new NotImplementedException();

  public string AggregateType => throw new NotImplementedException();

  public string EventType => throw new NotImplementedException();

  public object Payload => throw new NotImplementedException();
}
