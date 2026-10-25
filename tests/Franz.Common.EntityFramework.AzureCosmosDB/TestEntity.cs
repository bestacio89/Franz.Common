using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;


namespace Franz.Common.AzureCosmosDB.Tests;



public class CosmosEntity : Entity<Guid>
{
  protected CosmosEntity(Guid id) : base(id) { }
  public string Label { get; set; } = string.Empty;
}



