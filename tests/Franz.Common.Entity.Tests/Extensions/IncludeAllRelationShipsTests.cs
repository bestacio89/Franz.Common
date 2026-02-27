using Franz.Common.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Extensions;

public class IncludeAllRelationshipsTests
{
  private class Parent
  {
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public ICollection<Child> Children { get; set; } = new List<Child>();
  }

  private class Child
  {
    public int Id { get; set; }
    public string Value { get; set; } = default!;
    public int ParentId { get; set; }
    public Parent Parent { get; set; } = default!;
  }

  private class TestDbContext : DbContext
  {
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Child> Children => Set<Child>();
  }

  [Fact]
  public void IncludeAllRelationships_IncludesNavigationProperties()
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb")
        .Options;

    using var context = new TestDbContext(options);
    context.Parents.Add(new Parent { Id = 1, Name = "Parent1", Children = { new Child { Id = 1, Value = "Child1" } } });
    context.SaveChanges();

    var query = context.Parents.IncludeAllRelationships(context);
    var result = query.First();

    Assert.NotNull(result.Children);
    Assert.Single(result.Children);
    Assert.Equal("Child1", result.Children.First().Value);
  }
}