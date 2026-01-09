using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.AzureCosmosDB.Repositories;

public class CosmosAggregateRepository<TDbContext, TAggregateRoot, TEvent>
    : AggregateRepository<TDbContext, TAggregateRoot, TEvent>
    where TDbContext : CosmosDbContextBase
    where TAggregateRoot : class, IAggregateRoot<TEvent>, new()
    where TEvent : class, IDomainEvent
{
  public CosmosAggregateRepository(TDbContext dbContext, IDispatcher dispatcher)
      : base(dbContext, dispatcher)
  {
  }
}
