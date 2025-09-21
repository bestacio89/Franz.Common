namespace Franz.Common.Aras.Infrastructure.Persistence.Contexts
{
  internal record TrackedAggregate(object Aggregate, Type AggregateType, Type EventType);
}
