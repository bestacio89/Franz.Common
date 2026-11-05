#nullable enable
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka;

/// <summary>Resolves service-level Kafka topic names once per application service provider.</summary>
public sealed class KafkaTopicNames
{
  public KafkaTopicNames(IAssemblyAccessor assemblyAccessor, IOptions<KafkaMessagingOptions> options)
  {
    ArgumentNullException.ThrowIfNull(assemblyAccessor);
    ArgumentNullException.ThrowIfNull(options);
    var settings = options.Value;
    var entryAssembly = assemblyAccessor.GetEntryAssembly()
        ?? throw new InvalidOperationException("Entry assembly cannot be null.");

    TopicName = !string.IsNullOrWhiteSpace(settings.TopicName)
        ? settings.TopicName
        : TopicNamer.GetTopicName(entryAssembly);

    DeadLetterTopicName = !string.IsNullOrWhiteSpace(settings.Failure.DeadLetterTopic)
        ? settings.Failure.DeadLetterTopic
        : TopicNamer.GetDeadLetterTopicName(entryAssembly);
  }

  public string TopicName { get; }
  public string DeadLetterTopicName { get; }
}