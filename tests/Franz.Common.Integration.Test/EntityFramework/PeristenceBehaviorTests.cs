using FluentAssertions;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Behaviors;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework;
public class PersistenceBehaviorTests
{
  private sealed class Cmd : ICommand<string> { }
  private static readonly TestDispatcher Dispatcher = new();

  private sealed class Ctx : DbContextBase
  {
    public int Calls { get; private set; }
    public Ctx(DbContextOptions o, Franz.Common.Mediator.Dispatchers.IDispatcher d) : base(o, d) { }
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
      Calls++;
      return await base.SaveChangesAsync(ct);
    }
  }


  [Fact]
  public async Task Behavior_calls_SaveChanges_after_next()
  {
    var opts = new DbContextOptionsBuilder<Ctx>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    await using var ctx = new Ctx(opts, Dispatcher);

    var behavior = new PersistenceBehavior<Cmd, string>(ctx, NullLogger<PersistenceBehavior<Cmd, string>>.Instance);

    var result = await behavior.Handle(new Cmd(), () => Task.FromResult("ok"), default);

    result.Should().Be("ok");
    ctx.Calls.Should().BeGreaterThan(0);
  }
}