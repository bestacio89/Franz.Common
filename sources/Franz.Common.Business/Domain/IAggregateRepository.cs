using Franz.Common.Business.Domain;

public interface IAggregateRepository<TAggregateRoot> where TAggregateRoot : class, IAggregateRoot
{
}