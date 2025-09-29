using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Franz.Common.MongoDB.Configuration;

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
