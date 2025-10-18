using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Testing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework;
public class QueryFilterTests
{
  private sealed class Item : Entity<int>
  {
    public string Label { get; set; } = "";
  }

  private static readonly TestDispatcher Dispatcher = new();


  private sealed class Ctx : DbContextBase
  {
    public Ctx(DbContextOptions options, IDispatcher d) : base(options, d, currentUser: null) { }
    public DbSet<Item> Items => Set<Item>();
  }

  [Fact]
  public async Task Global_filter_excludes_soft_deleted_entities()
  {
    var opts = new DbContextOptionsBuilder<Ctx>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    await using var ctx = new Ctx(opts, Dispatcher);

    var a = new Item { Label = "a" };
    var b = new Item { Label = "b" };
    await ctx.AddRangeAsync(a, b);
    await ctx.SaveChangesAsync();

    // soft delete b
    ctx.Remove(b);
    await ctx.SaveChangesAsync();

    (await ctx.Items.AsNoTracking().ToListAsync()).Select(x => x.Label).Should().BeEquivalentTo("a");
    (await ctx.Items.IgnoreQueryFilters().ToListAsync()).Should().HaveCount(2);
  }
}
