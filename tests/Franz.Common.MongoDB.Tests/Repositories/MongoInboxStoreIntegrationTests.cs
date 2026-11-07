using Franz.Common.MongoDB.Repositories;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Franz.Common.MongoDB.Tests.Repositories;

public sealed class MongoInboxStoreIntegrationTests : IAsyncLifetime
{
  private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
  private MongoInboxStore _store = default!;

  public async Task InitializeAsync()
  {
    await _container.StartAsync();
    var client = new MongoClient(_container.GetConnectionString());
    var database = client.GetDatabase($"franz_inbox_tests_{Guid.NewGuid():N}");
    _store = new MongoInboxStore(database);
  }

  public Task DisposeAsync() => _container.DisposeAsync().AsTask();

  [Fact]
  public async Task TryRegisterAsync_OnlyOneConcurrentDeliveryWins()
  {
    var messageId = Guid.CreateVersion7();

    var outcomes = await Task.WhenAll(
        Enumerable.Range(0, 16)
            .Select(_ => _store.TryRegisterAsync(messageId)));

    Assert.Single(outcomes.Where(registered => registered));
    Assert.Equal(15, outcomes.Count(registered => !registered));
    Assert.True(await _store.HasProcessedAsync(messageId));
  }

  [Fact]
  public async Task TryRegisterAsync_RejectsEmptyMessageIdentifier()
  {
    await Assert.ThrowsAsync<ArgumentException>(
        () => _store.TryRegisterAsync(Guid.Empty));
  }

  [Fact]
  public async Task TryRegisterAsync_DoesNotTreatDifferentMessagesAsDuplicates()
  {
    var firstMessageId = Guid.CreateVersion7();
    var secondMessageId = Guid.CreateVersion7();

    Assert.True(await _store.TryRegisterAsync(firstMessageId));
    Assert.True(await _store.TryRegisterAsync(secondMessageId));
  }
}
