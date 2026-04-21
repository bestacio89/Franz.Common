using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;


namespace Franz.Common.AzureCosmosDB.Tests;



public class CosmosEntity : Entity<Guid>
{
  public string Label { get; set; } = string.Empty;
}



