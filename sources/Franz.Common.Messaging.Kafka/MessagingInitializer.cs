#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka;

/// <summary>
/// Kafka infrastructure initializer.
///
/// Supports two topic discovery modes:
///
/// SYSTEM MODE (perEvent: false) — default
///   One topic per service assembly. SkillService consuming HeroCreatedEvent
///   ensures "hero-in" exists (the source service topic).
///   Topic name derived from the event type's source assembly.
///
/// EVENT MODE (perEvent: true)
///   One topic per event type. HeroCreatedEvent → "hero-created-in".
///   Topic name derived from the event type name via kebab-case conversion.
///   Used when AddEventBasedKafkaMessaging is registered.
///
/// CRITICAL: _initialized is instance-level (not static) so that system mode
/// and event mode initializers are independent and both run correctly when
/// both modes are registered in the same process.
/// </summary>
public sealed class KafkaMessagingInitializer : IMessagingInitializer
{
  // Instance-level — NOT static.
  // Static would cause the second initializer (event mode) to be skipped
  // because the first (system mode) already set the flag.
  private int _initialized;
  private readonly SemaphoreSlim _initializationGate = new(1, 1);

  private readonly IAdminClient _adminClient;
  private readonly KafkaMessagingOptions _options;
  private readonly bool _perEvent;

  private readonly string _topicName;
  private readonly string _deadLetterTopicName;

  public KafkaMessagingInitializer(
      IAdminClient adminClient,
      KafkaTopicNames topicNames,
      IOptions<KafkaMessagingOptions> options,
      bool perEvent = false)
  {
    _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
    ArgumentNullException.ThrowIfNull(topicNames);
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _perEvent = perEvent;

    _topicName = topicNames.TopicName;
    _deadLetterTopicName = topicNames.DeadLetterTopicName;
  }

  public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
  {
    EnsureMessagingManifestRegistered();
    if (_options.ProvisioningMode == Franz.Common.Messaging.Configuration.ProvisioningMode.Off)
      return;

    await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (Volatile.Read(ref _initialized) == 1)
        return;

      ValidateReplicationFactorAgainstBrokerCount();
      await EnsureTopicAsync(_topicName, cancellationToken).ConfigureAwait(false);
      await EnsureTopicAsync(_deadLetterTopicName, cancellationToken).ConfigureAwait(false);

      foreach (var topic in DiscoverIntegrationEventTopics())
        await EnsureTopicAsync(topic, cancellationToken).ConfigureAwait(false);

      // Publish the initialized state only after every destination has been provisioned.
      // Failed and cancelled attempts remain retryable.
      Volatile.Write(ref _initialized, 1);
    }
    finally
    {
      _initializationGate.Release();
    }
  }

  private static void EnsureMessagingManifestRegistered()
  {
    if (Franz.Common.Messaging.Manifest.MessagingManifestRegistry.GetAll().Count == 0)
      throw new InvalidOperationException(
          "No generated messaging manifest is registered. Ensure the Franz mediator source generator is referenced and runs in the application compilation.");
  }

  private IReadOnlyCollection<string> DiscoverIntegrationEventTopics()
  {
    var eventTypes = Franz.Common.Messaging.Manifest.MessagingManifestRegistry.GetAll()
        .SelectMany(static manifest => manifest.Destinations)
        .Distinct()
        .ToArray();

    var topics = new HashSet<string>(StringComparer.Ordinal);
    foreach (var eventType in eventTypes)
    {
      var topic = _perEvent
          ? TopicNamer.GetTopicName(eventType)
          : TopicNamer.GetTopicName(new AssemblyWrapper(eventType.Assembly));
      var deadLetterTopic = _perEvent
          ? TopicNamer.GetDeadLetterTopicName(eventType)
          : TopicNamer.GetDeadLetterTopicName(new AssemblyWrapper(eventType.Assembly));

      if (!string.IsNullOrWhiteSpace(topic)) topics.Add(topic);
      if (!string.IsNullOrWhiteSpace(deadLetterTopic)) topics.Add(deadLetterTopic);
    }

    return topics.ToArray();
  }

  private void ValidateReplicationFactorAgainstBrokerCount()
  {
    var timeout = TimeSpan.FromMilliseconds(_options.Topic.CreationTimeoutMs);
    var brokerCount = _adminClient.GetMetadata(timeout).Brokers.Count;
    if (brokerCount < _options.Topic.ReplicationFactor)
      throw new InvalidOperationException(
          $"Kafka replication factor {_options.Topic.ReplicationFactor} exceeds the available broker count ({brokerCount}).");
  }

  private async ValueTask EnsureTopicAsync(string name, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(name))
      return;

    var topicOptions = _options.Topic;
    var timeout = TimeSpan.FromMilliseconds(topicOptions.CreationTimeoutMs);
    var specification = new TopicSpecification
    {
      Name = name,
      NumPartitions = topicOptions.NumPartitions,
      ReplicationFactor = topicOptions.ReplicationFactor,
      Configs = topicOptions.Configurations.Count == 0
          ? null
          : new Dictionary<string, string>(topicOptions.Configurations, StringComparer.Ordinal)
    };

    try
    {
      if (_options.ProvisioningMode == Franz.Common.Messaging.Configuration.ProvisioningMode.Verify)
      {
        var metadata = _adminClient.GetMetadata(name, timeout);
        var existingTopic = metadata.Topics.SingleOrDefault(topic =>
            string.Equals(topic.Topic, name, StringComparison.Ordinal));

        if (existingTopic is null || existingTopic.Error.Code != ErrorCode.NoError)
          throw new InvalidOperationException($"Required Kafka topic '{name}' does not exist or could not be inspected.");

        if (existingTopic.Partitions.Count < topicOptions.NumPartitions)
          throw new InvalidOperationException(
              $"Kafka topic '{name}' has {existingTopic.Partitions.Count} partitions; at least {topicOptions.NumPartitions} are required.");

        if (existingTopic.Partitions.Any(partition =>
            partition.Replicas.Count() != topicOptions.ReplicationFactor))
          throw new InvalidOperationException(
              $"Kafka topic '{name}' does not have the configured replication factor of {topicOptions.ReplicationFactor}.");

        return;
      }

      var creation = _adminClient.CreateTopicsAsync(
          new[] { specification },
          new CreateTopicsOptions
          {
            OperationTimeout = timeout,
            RequestTimeout = timeout
          });

      // The Admin API has no CancellationToken overload. Cancellation stops this
      // initializer from waiting or scheduling further topics; Kafka's request
      // timeout bounds the broker request.
      await creation.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
    catch (CreateTopicsException ex)
    {
      if (ex.Results.Any(result => result.Error.Code != ErrorCode.TopicAlreadyExists))
        throw;
    }
  }

  private static string GetCompanyPrefix(IAssembly entryAssembly)
  {
    var name = entryAssembly.Name ?? string.Empty;
    var first = name.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    return string.IsNullOrWhiteSpace(first) ? name : first;
  }
}