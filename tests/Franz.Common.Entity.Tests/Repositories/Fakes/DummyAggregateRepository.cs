using Franz.Common.EntityFramework.Repositories;
using Franz.Common.EntityFramework.Tests.Repositories.Fakes;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;

public class DummyAggregateRepository
    : AggregateRepository<
        TestDbContext3,
        DummyAggregate,
        DummyEvent,
        Guid>
{
  public DummyAggregateRepository(
      TestDbContext3 db,
      IDispatcher dispatcher)
      : base(db, dispatcher)
  {
  }

  protected override Task<DummyAggregate?> LoadAggregateAsync(
      Guid id,
      CancellationToken ct)
  {
    return DbContext
        .Set<DummyAggregate>()
        .FirstOrDefaultAsync(
            x => x.Id == id,
            ct);
  }
}