using Franz.Common.EntityFramework.Tests.Extensions.Dummies;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Repositories.Fakes;

public class TestDbContext3 : DbContextBase
{
  public TestDbContext3(DbContextOptions<TestDbContext3> options, IDispatcher dispatcher)
      : base(options, dispatcher)
  {
  }

  // Dummy DbSets for repository testing
  public DbSet<DummyAggregate> DummyAggregates { get; set; } = null!;
  public DbSet<DummyEntity> DummyEntities { get; set; } = null!;
  public DbSet<DummyEvent> DummyEvents { get; set; } = null!;
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ConvertEnumeration();
    
    modelBuilder.Entity<DummyEvent>()
        .HasKey(e => e.EventId);
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<DummyEntity>()
    .Property(x => x.EnumProp)
    .HasConversion(new EnumerationConverter<TestEnum, int>())
    .HasColumnName("EnumPropId");
  }

}