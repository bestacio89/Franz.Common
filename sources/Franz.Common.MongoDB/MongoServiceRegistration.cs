
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.EntityFramework;
using MongoDB.Driver;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, IConfiguration configuration) where TContext : MongoDbContext
  {
    var mongoDbSection = configuration.GetSection("MongoDb");
    var connectionString = mongoDbSection.GetValue<string>("ConnectionString");
    var databaseName = mongoDbSection.GetValue<string>("DatabaseName");

    services.AddScoped<IMongoClient>(s => new MongoClient(connectionString));
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    services.AddScoped<IMongoDatabase>(s => s.GetService<IMongoClient>().GetDatabase(databaseName));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    services.AddScoped<TContext>();

    return services;
  }
}
