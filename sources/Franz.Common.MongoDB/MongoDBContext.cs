using Microsoft.Extensions.Options;
using Franz.Common.MongoDB.config;
using MongoDB.Driver;

namespace Franz.Common.MongoDB;


public class MongoDbContext
{
  private readonly IMongoDatabase _database;

  public MongoDbContext(IMongoClient mongoClient, IOptions<MongoDBConfig> options)
  {
    _database = mongoClient.GetDatabase(options.Value.Database);
  }

  public IMongoCollection<T> GetCollection<T>(string name)
  {
    return _database.GetCollection<T>(name);
  }
}
