using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Franz.Common.EntityFramework.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Extensions.Dummies;


public class TestDbContext2 : DbContextBase
{
  public TestDbContext2(DbContextOptions options, IDispatcher dispatcher)
      : base(options, dispatcher) { }

  public DbSet<DummyEntity> Dummies { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    // This is the call that triggers your extension logic
    modelBuilder.ConvertEnumeration();
  }
}