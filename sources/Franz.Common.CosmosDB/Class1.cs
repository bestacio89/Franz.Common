using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MinervaFramework.Common.CosmosDB;
public static class ServiceRegistration
{
  public static void ConfigureCosmosDb(IServiceCollection services, string cosmosDbEndpoint, string cosmosDbKey, string cosmosDbDatabaseName)
  {
    var cosmosDbConfig = new CosmosDbConfig
    {
      Endpoint = cosmosDbEndpoint,
      Key = cosmosDbKey,
      DatabaseName = cosmosDbDatabaseName
    };

    services.AddDbContext<CosmosDbContext>(options =>
        options.UseCosmos(
            cosmosDbConfig.Endpoint,
            cosmosDbConfig.Key,
            cosmosDbConfig.DatabaseName));

    // Other services and dependencies registration
  }

  // Other service registration configurations
}