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
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly KafkaMessagingOptions _options;
  private readonly bool _perEvent;

  private readonly string _topicName;
  private readonly string _deadLetterTopicName;

  public KafkaMessagingInitializer(
      IAdminClient adminClient,
      IAssemblyAccessor assemblyAccessor,
      IOptions<KafkaMessagingOptions> options,
      bool perEvent = false)
  {
    _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
    _assemblyAccessor = assemblyAccessor ?? throw new ArgumentNullException(nameof(assemblyAccessor));
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _perEvent = perEvent;

    var entryAssembly = _assemblyAccessor.GetEntryAssembly();

    _topicName = !string.IsNullOrWhiteSpace(_options.TopicName)
        ? _options.TopicName
        : TopicNamer.GetTopicName(entryAssembly);

    _deadLetterTopicName = !string.IsNullOrWhiteSpace(_options.Failure.DeadLetterTopic)
        ? _options.Failure.DeadLetterTopic
        : TopicNamer.GetDeadLetterTopicName(entryAssembly);
  }

  public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
  {
    await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (Volatile.Read(ref _initialized) == 1)
        return;

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

  private IReadOnlyCollection<string> DiscoverIntegrationEventTopics()
  {
    var entryAssembly = _assemblyAccessor.GetEntryAssembly();
    var companyPrefix = GetCompanyPrefix(entryAssembly);

    var integrationEventTypes = AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a => !a.IsDynamic &&
                    !string.IsNullOrWhiteSpace(a.FullName) &&
                    a.FullName!.StartsWith(companyPrefix, StringComparison.OrdinalIgnoreCase))
        .SelectMany(a =>
        {
          try { return a.ExportedTypes; }
          catch { return Array.Empty<Type>(); }
        })
        .SelectMany(t => t.GetInterfaces())
        .Where(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
        .SelectMany(i => i.GenericTypeArguments)
        .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t))
        .Distinct()
        .ToList();

    var topics = new HashSet<string>(StringComparer.Ordinal);

    foreach (var evtType in integrationEventTypes)
    {
      var topic = _perEvent
          ? TopicNamer.GetTopicName(evtType)
          : TopicNamer.GetTopicName(new AssemblyWrapper(evtType.Assembly));

      var dlt = _perEvent
          ? TopicNamer.GetDeadLetterTopicName(evtType)
          : TopicNamer.GetDeadLetterTopicName(new AssemblyWrapper(evtType.Assembly));

      if (!string.IsNullOrWhiteSpace(topic)) topics.Add(topic);
      if (!string.IsNullOrWhiteSpace(dlt)) topics.Add(dlt);
    }

    return topics.ToList();
  }

  // =========================================================
  // INFRASTRUCTURE
  // =========================================================

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