using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;


namespace Franz.Common.EntityFramework.SQLServer.Tests;



public class TestEntity : Entity<Guid>
{
  public string Name { get; set; } = string.Empty;
}



