using Microting.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MariaDb;
using Xunit;

namespace Franz.Common.Integration.Test;

public sealed class MariaDbProviderIntegrationTests : IAsyncLifetime
{
  private readonly MariaDbContainer _database = new MariaDbBuilder("mariadb:11.4")
      .WithDatabase("franz_provider_tests")
      .WithUsername("franz_test")
      .WithPassword("franz_test_password")
      .Build();

  public Task InitializeAsync() => _database.StartAsync();

  public Task DisposeAsync() => _database.DisposeAsync().AsTask();

  [Fact]
  public async Task ProviderSupportsSchemaCrudRelationshipsQueriesAndTransactionRollback()
  {
    var options = new DbContextOptionsBuilder<MariaDbQualificationDbContext>()
        .UseMySql(
            _database.GetConnectionString(),
            ServerVersion.Create(new Version(11, 4, 0), ServerType.MariaDb))
        .Options;

    await using (var context = new MariaDbQualificationDbContext(options))
    {
      Assert.True(await context.Database.CanConnectAsync());
      Assert.True(await context.Database.EnsureCreatedAsync());

      var order = new MariaDbQualificationOrder
      {
        CustomerName = "Franz Provider Qualification",
        Lines =
        [
          new MariaDbQualificationLine { ProductName = "Architectural Book", UnitPrice = 24.95m },
          new MariaDbQualificationLine { ProductName = "Notebook", UnitPrice = 12.50m }
        ]
      };

      context.Orders.Add(order);
      Assert.Equal(3, await context.SaveChangesAsync());
      Assert.True(order.Id > 0);
      Assert.All(order.Lines, line => Assert.True(line.Id > 0));
    }

    await using (var context = new MariaDbQualificationDbContext(options))
    {
      var projection = await context.Orders
          .Where(order =>
              order.CustomerName.Contains("Provider") &&
              order.Lines.Any(line => line.UnitPrice >= 12.50m))
          .Select(order => new
          {
            order.Id,
            LineCount = order.Lines.Count
          })
          .SingleAsync();

      Assert.True(projection.Id > 0);
      Assert.Equal(2, projection.LineCount);

      var persistedOrder = await context.Orders
          .Include(order => order.Lines)
          .SingleAsync(order => order.Id == projection.Id);

      Assert.Equal("Franz Provider Qualification", persistedOrder.CustomerName);
      Assert.Equal(2, persistedOrder.Lines.Count);
      Assert.Contains(persistedOrder.Lines, line => line.ProductName == "Architectural Book");
    }

    int rolledBackOrderId;
    await using (var context = new MariaDbQualificationDbContext(options))
    await using (var transaction = await context.Database.BeginTransactionAsync())
    {
      var rolledBackOrder = new MariaDbQualificationOrder
      {
        CustomerName = "Transaction Rollback"
      };

      context.Orders.Add(rolledBackOrder);
      await context.SaveChangesAsync();
      rolledBackOrderId = rolledBackOrder.Id;
      await transaction.RollbackAsync();
    }

    await using (var context = new MariaDbQualificationDbContext(options))
    {
      Assert.False(await context.Orders.AnyAsync(order => order.Id == rolledBackOrderId));
      Assert.Equal(1, await context.Orders.CountAsync());
    }
  }
}

internal sealed class MariaDbQualificationDbContext(
    DbContextOptions<MariaDbQualificationDbContext> options)
    : DbContext(options)
{
  public DbSet<MariaDbQualificationOrder> Orders => Set<MariaDbQualificationOrder>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<MariaDbQualificationOrder>(entity =>
    {
      entity.ToTable("provider_qualification_orders");
      entity.HasKey(order => order.Id);
      entity.Property(order => order.CustomerName)
          .HasMaxLength(160)
          .IsRequired();
      entity.HasMany(order => order.Lines)
          .WithOne(line => line.Order)
          .HasForeignKey(line => line.OrderId)
          .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<MariaDbQualificationLine>(entity =>
    {
      entity.ToTable("provider_qualification_lines");
      entity.HasKey(line => line.Id);
      entity.Property(line => line.ProductName)
          .HasMaxLength(160)
          .IsRequired();
      entity.Property(line => line.UnitPrice)
          .HasPrecision(18, 2);
    });
  }
}

internal sealed class MariaDbQualificationOrder
{
  public int Id { get; set; }

  public string CustomerName { get; set; } = string.Empty;

  public List<MariaDbQualificationLine> Lines { get; set; } = [];
}

internal sealed class MariaDbQualificationLine
{
  public int Id { get; set; }

  public int OrderId { get; set; }

  public string ProductName { get; set; } = string.Empty;

  public decimal UnitPrice { get; set; }

  public MariaDbQualificationOrder Order { get; set; } = null!;
}
