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

public sealed class KafkaMessagingInitializer : IMessagingInitializer
{
  private static int _initialized;

  private readonly IAdminClient _adminClient;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly KafkaMessagingOptions _options;

  private readonly string _topicName;
  private readonly string _deadLetterTopicName;

  public KafkaMessagingInitializer(
      IAdminClient adminClient,
      IAssemblyAccessor assemblyAccessor,
      IOptions<KafkaMessagingOptions> options)
  {
    _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
    _assemblyAccessor = assemblyAccessor ?? throw new ArgumentNullException(nameof(assemblyAccessor));
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

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
    if (Interlocked.Exchange(ref _initialized, 1) == 1)
      return;

    await EnsureTopicAsync(_topicName, cancellationToken).ConfigureAwait(false);
    await EnsureTopicAsync(_deadLetterTopicName, cancellationToken).ConfigureAwait(false);

    foreach (var topic in DiscoverIntegrationEventTopics())
    {
      await EnsureTopicAsync(topic, cancellationToken).ConfigureAwait(false);
    }
  }

  private async ValueTask EnsureTopicAsync(string name, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(name))
      return;

    // No options for partitions/replication; default to 1
    try
    {
      await _adminClient.CreateTopicsAsync(new[]
      {
                new TopicSpecification
                {
                    Name = name,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            }).ConfigureAwait(false);
    }
    catch (CreateTopicsException ex)
    {
      if (ex.Results.Any(r => r.Error.Code != ErrorCode.TopicAlreadyExists))
        throw;
    }
  }

  private IReadOnlyCollection<string> DiscoverIntegrationEventTopics()
  {
    var entryAssembly = _assemblyAccessor.GetEntryAssembly();
    var companyPrefix = GetCompanyPrefix(entryAssembly);

    var integrationEventTypes = AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName) && a.FullName!.StartsWith(companyPrefix))
        .SelectMany(a =>
        {
          try { return a.ExportedTypes; }
          catch { return Array.Empty<Type>(); }
        })
        .SelectMany(t => t.GetInterfaces())
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
        .SelectMany(i => i.GenericTypeArguments)
        .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t))
        .Distinct()
        .ToList();

    var topics = new HashSet<string>(StringComparer.Ordinal);
    foreach (var evtType in integrationEventTypes)
    {
      var assemblyWrapper = new AssemblyWrapper(evtType.Assembly);
      var perEventTopic = TopicNamer.GetTopicName(assemblyWrapper);
      if (!string.IsNullOrWhiteSpace(perEventTopic))
        topics.Add(perEventTopic);
    }

    return topics.ToList();
  }

  private static string GetCompanyPrefix(IAssembly entryAssembly)
  {
    var name = entryAssembly.Name ?? string.Empty;
    var first = name.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    return string.IsNullOrWhiteSpace(first) ? name : first;
  }
}