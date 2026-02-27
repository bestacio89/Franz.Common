using Franz.Common.Mediator.Dispatchers;
using global::Franz.Common.EntityFramework.Repositories;
using global::Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;


namespace Franz.Common.EntityFramework.Tests.Repositories.Fakes;




public class DummyAggregateRepository : AggregateRepository<TestDbContext3, DummyAggregate, DummyEvent>
{
  public DummyAggregateRepository(TestDbContext3 dbContext, IDispatcher dispatcher)
      : base(dbContext, dispatcher)
  {

  }
}