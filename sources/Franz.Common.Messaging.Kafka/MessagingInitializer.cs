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
/// Provisions service, event and dead-letter topics from the generated messaging manifest.
/// </summary>
public sealed class KafkaMessagingInitializer : IMessagingInitializer
{
  private readonly SemaphoreSlim _initializationGate = new(1, 1);
  private readonly IAdminClient _adminClient;
  private readonly KafkaMessagingOptions _options;
  private readonly bool _perEvent;
  private readonly string _topicName;
  private readonly string _deadLetterTopicName;
  private int _initialized;

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
    if (_options.ProvisioningMode == Franz.Common.Messaging.Configuration.ProvisioningMode.Off)
      return;

    await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (Volatile.Read(ref _initialized) == 1)
        return;

      var topicPlans = new Dictionary<string, KafkaTopicOptions>(StringComparer.Ordinal)
      {
        [_topicName] = _options.Topic,
        [_deadLetterTopicName] = _options.DeadLetterTopicOptions ?? _options.Topic
      };

      foreach (var topic in DiscoverIntegrationEventTopics())
        topicPlans.TryAdd(topic.Name, topic.Options);

      ValidateReplicationFactorAgainstBrokerCount(topicPlans);
      foreach (var (topicName, topicOptions) in topicPlans)
        await EnsureTopicAsync(topicName, topicOptions, cancellationToken).ConfigureAwait(false);

      // Publish initialized state only after every destination has been provisioned.
      // Failed and cancelled attempts remain retryable.
      Volatile.Write(ref _initialized, 1);
    }
    finally
    {
      _initializationGate.Release();
    }
  }

  private IReadOnlyCollection<TopicPlan> DiscoverIntegrationEventTopics()
  {
    var eventTypes = Franz.Common.Messaging.Manifest.MessagingManifestRegistry.GetAll()
        .SelectMany(static manifest => manifest.Destinations)
        .Distinct()
        .ToArray();

    var topics = new Dictionary<string, KafkaTopicOptions>(StringComparer.Ordinal);
    foreach (var eventType in eventTypes)
    {
      var topicName = _perEvent
          ? TopicNamer.GetTopicName(eventType)
          : TopicNamer.GetTopicName(new AssemblyWrapper(eventType.Assembly));
      var deadLetterTopicName = _perEvent
          ? TopicNamer.GetDeadLetterTopicName(eventType)
          : TopicNamer.GetDeadLetterTopicName(new AssemblyWrapper(eventType.Assembly));

      if (string.IsNullOrWhiteSpace(topicName))
        continue;

      var eventTopicOptions = ResolveEventTopicOptions(topicName);
      topics.TryAdd(topicName, eventTopicOptions);

      if (!string.IsNullOrWhiteSpace(deadLetterTopicName))
        topics.TryAdd(deadLetterTopicName, _options.DeadLetterTopicOptions ?? eventTopicOptions);
    }

    return topics.Select(static pair => new TopicPlan(pair.Key, pair.Value)).ToArray();
  }

  private KafkaTopicOptions ResolveEventTopicOptions(string topicName)
      => _options.EventTopicOptions.TryGetValue(topicName, out var topicOptions)
          ? topicOptions
          : _options.Topic;

  private void ValidateReplicationFactorAgainstBrokerCount(
      IReadOnlyDictionary<string, KafkaTopicOptions> topicPlans)
  {
    var timeoutMilliseconds = topicPlans.Values.Max(static topic => topic.CreationTimeoutMs);
    var brokerCount = _adminClient.GetMetadata(TimeSpan.FromMilliseconds(timeoutMilliseconds)).Brokers.Count;
    var invalidTopics = topicPlans
        .Where(pair => pair.Value.ReplicationFactor > brokerCount)
        .Select(pair => $"'{pair.Key}' requests replication factor {pair.Value.ReplicationFactor}")
        .ToArray();

    if (invalidTopics.Length > 0)
      throw new InvalidOperationException(
          $"Kafka topic replication factor exceeds the available broker count ({brokerCount}) for: {string.Join(", ", invalidTopics)}.");
  }

  private async ValueTask EnsureTopicAsync(
      string name,
      KafkaTopicOptions topicOptions,
      CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(name))
      return;

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

    if (_options.ProvisioningMode == Franz.Common.Messaging.Configuration.ProvisioningMode.Verify)
    {
      VerifyTopic(name, topicOptions, timeout);
      return;
    }

    try
    {
      await _adminClient.CreateTopicsAsync(
          new[] { specification },
          new CreateTopicsOptions
          {
            OperationTimeout = timeout,
            RequestTimeout = timeout
          }).WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
    catch (CreateTopicsException ex) when (
        ex.Results.All(static result => result.Error.Code == ErrorCode.TopicAlreadyExists))
    {
      // Existing topics are reconciled below. Kafka permits increasing, but not
      // decreasing, a topic's partition count.
    }

    await EnsureMinimumPartitionCountAsync(name, topicOptions, timeout, cancellationToken)
        .ConfigureAwait(false);
  }

  private void VerifyTopic(string name, KafkaTopicOptions topicOptions, TimeSpan timeout)
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
  }

  private async Task EnsureMinimumPartitionCountAsync(
      string name,
      KafkaTopicOptions topicOptions,
      TimeSpan timeout,
      CancellationToken cancellationToken)
  {
    var metadata = _adminClient.GetMetadata(name, timeout);
    var existingTopic = metadata.Topics.SingleOrDefault(topic =>
        string.Equals(topic.Topic, name, StringComparison.Ordinal));

    if (existingTopic is null || existingTopic.Error.Code != ErrorCode.NoError)
      throw new InvalidOperationException($"Kafka topic '{name}' was not available after provisioning.");

    var currentPartitionCount = existingTopic.Partitions.Count;
    if (currentPartitionCount >= topicOptions.NumPartitions)
      return;

    await _adminClient.CreatePartitionsAsync(
        new[]
        {
          new PartitionsSpecification
          {
            Topic = name,
            IncreaseTo = topicOptions.NumPartitions
          }
        },
        new CreatePartitionsOptions
        {
          OperationTimeout = timeout,
          RequestTimeout = timeout
        }).WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
  }

  private sealed record TopicPlan(string Name, KafkaTopicOptions Options);
}