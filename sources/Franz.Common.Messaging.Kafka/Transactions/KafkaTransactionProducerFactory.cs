#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Connections;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Kafka.Configuration;

namespace Franz.Common.Messaging.Kafka.Transactions;

/// <summary>Creates an isolated transactional producer for one transaction scope.</summary>
public sealed class KafkaTransactionProducerFactory : IKafkaTransactionProducerFactory
{
  private readonly IConnectionFactoryProvider _configurationProvider;
  private readonly KafkaMessagingOptions _options;

  public KafkaTransactionProducerFactory(
      IConnectionFactoryProvider configurationProvider,
      IOptions<KafkaMessagingOptions> options)
  {
    _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
  }

  public IProducer<string, byte[]> Create()
  {
    var config = _configurationProvider.Current;
    var prefix = string.IsNullOrWhiteSpace(_options.Producer.TransactionalIdPrefix)
        ? "franz"
        : _options.Producer.TransactionalIdPrefix.Trim();
    config.TransactionalId = $"{prefix}-{Environment.MachineName}-{Guid.NewGuid():N}";
    config.TransactionTimeoutMs = _options.Producer.TransactionTimeoutMs;

    var producer = new ProducerBuilder<string, byte[]>(config)
        .SetKeySerializer(Serializers.Utf8)
        .SetValueSerializer(Serializers.ByteArray)
        .Build();

    try
    {
      producer.InitTransactions(TimeSpan.FromSeconds(30));
      return producer;
    }
    catch
    {
      producer.Dispose();
      throw;
    }
  }
}
