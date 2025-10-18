using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Extensions;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework;
public class IncludeAllRelationshipsTests
{
  private sealed class Order : Entity<int>
  {
    public string Code { get; set; } = "";
    public List<OrderLine> Lines { get; set; } = new();
  }

  private sealed class OrderLine : Entity<int>
  {
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
  }

  private static readonly TestDispatcher Dispatcher = new();


  private sealed class C : DbContextBase
  {
    public C(DbContextOptions o, IDispatcher d) : base(o, d) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> Lines => Set<OrderLine>();
  }

  [Fact]
  public async Task IncludeAllRelationships_should_include_navigations()
  {
    var opts = new DbContextOptionsBuilder<C>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    await using var ctx = new C(opts, Dispatcher);

    var order = new Order { Code = "O1", Lines = [new OrderLine { Sku = "SKU-1", Quantity = 3 }] };
    await ctx.AddAsync(order);
    await ctx.SaveChangesAsync();

    var loaded = await ctx.Orders.IncludeAllRelationships(ctx).FirstAsync();
    loaded.Lines.Should().HaveCount(1);
    loaded.Lines.First().Sku.Should().Be("SKU-1");
  }
}