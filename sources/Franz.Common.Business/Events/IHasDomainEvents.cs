using Franz.Common.Business.Events;

namespace Franz.Common.Business.Domain
{
  public interface IHasDomainEvents
  {
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void AddDomainEvent(IDomainEvent @event);

    void ClearDomainEvents();
  }
}