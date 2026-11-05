#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Transactions;

[Collection("KafkaTransactionIntegration")]
public sealed class KafkaTransactionIntegrationTests : IClassFixture<KafkaContainerFixture>
{
  private readonly KafkaContainerFixture _fixture;

  public KafkaTransactionIntegrationTests(KafkaContainerFixture fixture)
      => _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

  [Fact]
  public async Task CompleteAsync_ShouldExposeCommittedMessagesToReadCommittedConsumers()
  {
    KafkaMessagingCapabilities.Descriptor.Supports(MessagingCapability.TransactionalPublishing)
        .Should().BeTrue("the adapter must exercise its declared transactional publishing capability");
    using var provider = (ServiceProvider)_fixture.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;
    options.Producer.TransactionalIdPrefix = $"franz-commit-{Guid.CreateVersion7():N}";

    var accessor = scope.ServiceProvider.GetRequiredService<IAssemblyAccessor>();
    var serializer = scope.ServiceProvider.GetRequiredService<IMessageSerializer>();
    var topic = scope.ServiceProvider.GetRequiredService<KafkaTopicNames>().TopicName;
    var messageId = Guid.CreateVersion7();
    var context = new KafkaTransactionContext();

    using var consumer = CreateReadCommittedConsumer(topic);

    await using var transaction = CreateTransaction(options, context);
    await using var sender = CreateSender(serializer, accessor, options, context);

    await transaction.BeginAsync();
    await sender.SendAsync(new Message($"committed-{messageId:N}") { Id = messageId });
    await transaction.CompleteAsync();

    var received = ConsumeUntilMessageId(consumer, messageId, TimeSpan.FromSeconds(10));
    received.Should().NotBeNull("a committed transactional record must be visible to read-committed consumers");
  }

  [Fact]
  public async Task RollbackAsync_ShouldHideAbortedMessagesFromReadCommittedConsumers()
  {
    using var provider = (ServiceProvider)_fixture.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;
    options.Producer.TransactionalIdPrefix = $"franz-abort-{Guid.CreateVersion7():N}";

    var accessor = scope.ServiceProvider.GetRequiredService<IAssemblyAccessor>();
    var serializer = scope.ServiceProvider.GetRequiredService<IMessageSerializer>();
    var topic = TopicNamer.GetTopicName(accessor.GetEntryAssembly());
    var messageId = Guid.CreateVersion7();
    var context = new KafkaTransactionContext();

    using var consumer = CreateReadCommittedConsumer(topic);
    _ = consumer.Consume(TimeSpan.FromSeconds(1));

    await using var transaction = CreateTransaction(options, context);
    await using var sender = CreateSender(serializer, accessor, options, context);

    await transaction.BeginAsync();
    await sender.SendAsync(new Message($"aborted-{messageId:N}") { Id = messageId });
    await transaction.RollbackAsync();

    var received = ConsumeUntilMessageId(consumer, messageId, TimeSpan.FromSeconds(3));
    received.Should().BeNull("an aborted transactional record must not be visible to read-committed consumers");
  }

  private static MessagingTransaction CreateTransaction(
      KafkaMessagingOptions options,
      KafkaTransactionContext context)
  {
    var wrappedOptions = Options.Create(options);
    var configurationProvider = new global::ConnectionFactoryProvider(wrappedOptions);
    var producerFactory = new KafkaTransactionProducerFactory(configurationProvider, wrappedOptions);
    return new MessagingTransaction(producerFactory, context);
  }

  private static KafkaSender CreateSender(
      IMessageSerializer serializer,
      IAssemblyAccessor accessor,
      KafkaMessagingOptions options,
      KafkaTransactionContext context)
      => new(
          Mock.Of<IProducer<string, byte[]>>(),
          serializer,
          new KafkaTopicNames(accessor, Options.Create(options)),
          Options.Create(options),
          NullLogger<KafkaSender>.Instance,
          context);

  private IConsumer<string, byte[]> CreateReadCommittedConsumer(string topic)
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = _fixture.BootstrapServers,
      GroupId = $"read-committed-{Guid.CreateVersion7():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false,
      IsolationLevel = IsolationLevel.ReadCommitted
    };

    var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
    // Explicit partition assignment avoids a group-join/offset-reset race in this
    // transaction-isolation test. The test filters the unique message key, so it
    // remains isolated from records produced by other integration tests.
    consumer.Assign(new TopicPartition(topic, new Partition(0)));
    return consumer;
  }

  private static ConsumeResult<string, byte[]>? ConsumeUntilMessageId(
      IConsumer<string, byte[]> consumer,
      Guid messageId,
      TimeSpan timeout)
  {
    var deadline = DateTimeOffset.UtcNow + timeout;
    while (DateTimeOffset.UtcNow < deadline)
    {
      var result = consumer.Consume(TimeSpan.FromMilliseconds(200));
      if (result?.Message is { } message &&
          string.Equals(message.Key, messageId.ToString(), StringComparison.Ordinal))
      {
        return result;
      }
    }

    return null;
  }
}
