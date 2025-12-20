using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Franz.Common.AzureCosmosDB.Storage;

public class CosmosDBMessageStore : IMessageStore
{
  private readonly Container _outbox;
  private readonly Container _deadletter;

  public CosmosDBMessageStore(Database database,
      string outboxContainer = "OutboxMessages",
      string deadLetterContainer = "DeadLetterMessages")
  {
    _outbox = database.CreateContainerIfNotExistsAsync(outboxContainer, "/id")
                      .GetAwaiter().GetResult();
    _deadletter = database.CreateContainerIfNotExistsAsync(deadLetterContainer, "/id")
                          .GetAwaiter().GetResult();
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    var stored = message.ToStored();
    await _outbox.CreateItemAsync(stored, new PartitionKey(stored.Id), cancellationToken: cancellationToken);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    var query = _outbox.GetItemLinqQueryable<StoredMessage>()
                       .Where(m => m.SentOn == null)
                       .ToFeedIterator();

    var results = new List<StoredMessage>();
    while (query.HasMoreResults)
    {
      var response = await query.ReadNextAsync(cancellationToken);
      results.AddRange(response);
    }
    return results;
  }

  public async Task MarkAsSentAsync(string messageId, CancellationToken cancellationToken = default)
  {
    var patch = new[]
    {
            PatchOperation.Set("/SentOn", DateTime.UtcNow)
        };

    await _outbox.PatchItemAsync<StoredMessage>(
        id: messageId,
        partitionKey: new PartitionKey(messageId),
        patchOperations: patch,
        cancellationToken: cancellationToken
    );
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    // TODO: could use PatchItemAsync for minimal updates, same as MarkAsSentAsync
    await _outbox.ReplaceItemAsync(message, message.Id, new PartitionKey(message.Id), cancellationToken: cancellationToken);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    await _deadletter.CreateItemAsync(message, new PartitionKey(message.Id), cancellationToken: cancellationToken);
    await _outbox.DeleteItemAsync<StoredMessage>(message.Id, new PartitionKey(message.Id), cancellationToken: cancellationToken);
  }
}
