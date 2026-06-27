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

namespace Franz.Common.EntityFramework.Tests;
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
    // 1. Arrange: Use a unique ID for the DB name to ensure isolation
    var opts = new DbContextOptionsBuilder<Ctx>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    // Use one context for the "Write"
    using (var ctx = new Ctx(opts, Dispatcher))
    {
      var a = new Item { Label = "a" };
      var b = new Item { Label = "b" };
      await ctx.AddRangeAsync(a, b);
      await ctx.SaveChangesAsync();

      // Act: soft delete b
      ctx.Remove(b);
      await ctx.SaveChangesAsync();
    } // ctx is disposed here, clearing the memory cache

    // 2. Assert: Use a second context for the "Read"
    using (var ctx = new Ctx(opts, Dispatcher))
    {
      var results = await ctx.Items.ToListAsync();

      results.Select(x => x.Label).Should().ContainSingle()
          .Which.Should().Be("a");

      // 3. Verify the soft delete physically exists
      var allItems = await ctx.Items.IgnoreQueryFilters().ToListAsync();
      allItems.Should().HaveCount(2); // 'a' and 'b' should both be there
      allItems.Should().Contain(x => x.Label == "b" && x.IsDeleted);
    }
  }
}
