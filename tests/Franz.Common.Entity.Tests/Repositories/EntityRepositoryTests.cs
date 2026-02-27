using Franz.Common.EntityFramework.Repositories;
using Franz.Common.EntityFramework.Tests.Extensions.Dummies;
using Franz.Common.EntityFramework.Tests.Repositories.Fakes;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Repositories;

public class EntityRepositoryTests
{
  [Fact]
  public async Task AddAndGetEntity_Works()
  {
    var options = new DbContextOptionsBuilder<TestDbContext3>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    using var context = new TestDbContext3(options, new Mock<IDispatcher>().Object);
    var repo = new EntityRepository<TestDbContext3, DummyEntity>(context);

    var entity = new DummyEntity();
    await repo.AddAsync(entity);

    var fetched = await repo.GetByIdAsync(entity.Id);

    Assert.Equal(entity.Id, fetched.Id);
  }
}