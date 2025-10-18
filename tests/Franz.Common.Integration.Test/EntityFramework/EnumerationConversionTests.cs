using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading.Tasks;

namespace Franz.Common.Integration.Tests.EntityFramework
{
  public class EnumerationConversionTests
  {
    // --- Enumeration Definition ---
    public sealed class OrderStatus : Enumeration<int>
    {
      public static readonly OrderStatus Pending = new(1, nameof(Pending));
      public static readonly OrderStatus Completed = new(2, nameof(Completed));
      public OrderStatus(int id, string name) : base(id, name) { }
    }

    // --- Entity Definition ---
    private sealed class OrderEntity : Entity<int>
    {
      public string Code { get; set; } = "";
      public OrderStatus Status { get; set; } = OrderStatus.Pending;
    }

    // --- DbContext Setup ---
    private sealed class OrdersCtx : DbContextBase
    {
      public OrdersCtx(DbContextOptions options, IDispatcher dispatcher) : base(options, dispatcher) { }

      public DbSet<OrderEntity> Orders => Set<OrderEntity>();

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        base.OnModelCreating(modelBuilder);

        // Apply the EnumerationConverter<TEnum, TId> explicitly
        modelBuilder
          .Entity<OrderEntity>()
          .Property(x => x.Status)
          .HasConversion(new EnumerationConverter<OrderStatus, int>());
      }
    }

    private static OrdersCtx NewContext()
    {
      var opts = new DbContextOptionsBuilder<OrdersCtx>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

      return new OrdersCtx(opts, new TestDispatcher());
    }

    // --- Test ---
    [Fact]
    public async Task Enumeration_roundtrips_through_converter()
    {
      await using var ctx = NewContext();

      var order = new OrderEntity { Code = "ORD-001", Status = OrderStatus.Completed };
      await ctx.Orders.AddAsync(order);
      await ctx.SaveChangesAsync();

      var again = await ctx.Orders.AsNoTracking().FirstAsync();

      again.Status.Should().Be(OrderStatus.Completed);
      again.Status.Id.Should().Be(2);
      again.Status.Name.Should().Be(nameof(OrderStatus.Completed));
    }
  }
}
