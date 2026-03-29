using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

public sealed class MongoContainerFixture : IAsyncLifetime
{
  private readonly MongoDbContainer _container =
    new MongoDbBuilder("mongo:7.0")
      .WithCleanUp(true)
      .Build();

  public string ConnectionString => _container.GetConnectionString();
  public string DatabaseName { get; } = $"franz-tests-{Guid.NewGuid():N}";

  public async Task InitializeAsync()
    => await _container.StartAsync();
  public Dictionary<string, string?> GetConfiguration()
  {
    return new Dictionary<string, string?>
    {
      ["Mongo:ConnectionString"] = ConnectionString,
      ["Mongo:Database"] = DatabaseName
    };
  }
  public async Task DisposeAsync()
    => await _container.DisposeAsync();
}
