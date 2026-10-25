using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;


namespace Franz.Common.EntityFramework.SQLServer.Tests;



public class TestEntity : Entity<Guid>
{
  protected TestEntity(Guid id) : base(id) { }
  public string Name { get; set; } = string.Empty;
}



