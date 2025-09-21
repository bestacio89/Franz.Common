namespace Franz.Common.Business.Domain;

public interface IAggregateRoot
{
  IReadOnlyCollection<BaseDomainEvent> GetUncommittedChanges();
  void MarkChangesAsCommitted();
  Guid Id { get; }
}
