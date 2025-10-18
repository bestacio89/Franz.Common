using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Testing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;

namespace Franz.Common.Integration.Tests.EntityFramework;
public class DbContextBaseAuditingTests
{
  private sealed class AuditOrder : Entity<int>
  {
    public string Name { get; set; } = "";
  }

  private static readonly TestDispatcher Dispatcher = new();

  private sealed class StubUser(string? id) : ICurrentUserService { public string? UserId => id; }

  private sealed class AuditDbContext : DbContextBase
  {
    public int SaveCalls { get; private set; }
    public DbSet<AuditOrder> Orders => Set<AuditOrder>();

    public AuditDbContext(DbContextOptions options, IDispatcher d, ICurrentUserService u) : base(options, d, u) { }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
      SaveCalls++;
      return await base.SaveChangesAsync(cancellationToken);
    }
  }

  private static AuditDbContext NewContext(ICurrentUserService user)
  {
    var opts = new DbContextOptionsBuilder<AuditDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    return new AuditDbContext(opts, new TestDispatcher(), user);
  }

  [Fact]
  public async Task Create_Update_Delete_set_audit_fields_and_soft_delete()
  {
    var ctx = NewContext(new StubUser("user-1"));

    var o = new AuditOrder { Name = "A" };
    await ctx.Orders.AddAsync(o);
    await ctx.SaveChangesAsync();

    o.CreatedBy.Should().Be("user-1");
    o.DateCreated.Should().NotBe(default);

    o.Name = "B";
    await ctx.SaveChangesAsync();
    o.LastModifiedBy.Should().Be("user-1");
    o.LastModifiedDate.Should().NotBe(default);

    ctx.Orders.Remove(o);
    await ctx.SaveChangesAsync();

    o.IsDeleted.Should().BeTrue();
    o.DateDeleted.Should().NotBeNull();
    // Soft delete means entity is still present as Modified.
    ctx.SaveCalls.Should().BeGreaterThan(0);
  }
}