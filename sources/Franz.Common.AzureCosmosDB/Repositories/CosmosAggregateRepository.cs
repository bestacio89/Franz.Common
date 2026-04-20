using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.Mediator.Dispatchers;

namespace Franz.Common.AzureCosmosDB.Repositories;

/// <summary>
/// Cosmos-specific aggregate repository.
/// 
/// Reuses EF aggregate orchestration patterns, but executes under CosmosDbContext semantics:
/// - no relational unit-of-work guarantees
/// - persistence governed by Cosmos provider execution model
/// - event dispatch remains domain-driven, not transaction-driven
/// </summary>
public class CosmosAggregateRepository<
    TDbContext,
    TAggregateRoot,
    TEvent,
    TId>
    : AggregateRepository<
        TDbContext,
        TAggregateRoot,
        TEvent,
        TId>
    where TDbContext : CosmosDbContextBase
    where TAggregateRoot : AggregateRoot<TEvent>
    where TEvent : class, IDomainEvent
{
  public CosmosAggregateRepository(
      TDbContext dbContext,
      IDispatcher dispatcher)
      : base(dbContext, dispatcher)
  {
  }

  // Optional: Cosmos-specific overrides later
  // e.g. partition-aware aggregate loading, batching, etc.
}