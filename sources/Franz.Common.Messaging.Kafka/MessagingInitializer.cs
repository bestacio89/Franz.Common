#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages; // INotificationHandler<>
using Franz.Common.Messaging.Configuration;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka;

public sealed class MessagingInitializer : IMessagingInitializer
{
  // NOTE:
  // Static "initialized" is process-wide. It's fine for a hosted service scenario,
  // but if you want per-host isolation in tests, consider making it instance-scoped.
  private static int _initialized = 0;

  private readonly IAdminClient _adminClient;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly MessagingOptions _options;

  private readonly string _topicName;
  private readonly string _deadLetterTopicName;

  public MessagingInitializer(
    IAdminClient adminClient,
    IAssemblyAccessor assemblyAccessor,
    IOptions<MessagingOptions> messagingOptions)
  {
    _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
    _assemblyAccessor = assemblyAccessor ?? throw new ArgumentNullException(nameof(assemblyAccessor));
    _options = messagingOptions?.Value ?? throw new ArgumentNullException(nameof(messagingOptions));

    // Franz abstraction (IAssembly)
    var entry = _assemblyAccessor.GetEntryAssembly();

    // These MUST accept IAssembly (Franz), not System.Reflection.Assembly.
    _topicName = TopicNamer.GetTopicName(entry);
    _deadLetterTopicName = TopicNamer.GetDeadLetterTopicName(entry);
  }

  /// <summary>
  /// Initializes topics used by Franz Kafka messaging (main + DLQ + subscription topics).
  /// </summary>
  public void Initialize()
  {
    // Keep sync signature (IMessagingInitializer), but do correct async internally.
    InitializeAsync().GetAwaiter().GetResult();
  }

  private async Task InitializeAsync(CancellationToken ct = default)
  {
    // Ensure only one initializer runs per process.
    if (Interlocked.Exchange(ref _initialized, 1) == 1)
      return;

    await EnsureTopicAsync(_topicName, ct).ConfigureAwait(false);
    await EnsureTopicAsync(_deadLetterTopicName, ct).ConfigureAwait(false);

    var subscriptionTopics = DiscoverIntegrationEventTopics();

    foreach (var topic in subscriptionTopics)
      await EnsureTopicAsync(topic, ct).ConfigureAwait(false);
  }

  /// <summary>
  /// Creates a topic if it doesn't already exist.
  /// Swallows "already exists" errors to keep init idempotent.
  /// </summary>
  private async Task EnsureTopicAsync(string name, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(name))
      return;

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
      // Confluent reports per-topic results; "TopicAlreadyExists" is normal on reruns.
      var nonAlreadyExists = ex.Results
        .Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists)
        .ToList();

      if (nonAlreadyExists.Count == 0)
        return;

      // If anything else happened, rethrow.
      throw;
    }
  }

  /// <summary>
  /// Discovers all integration event types referenced by INotificationHandler&lt;T&gt; across the app domain,
  /// then maps them to per-event Kafka topic names.
  /// </summary>
  private IReadOnlyCollection<string> DiscoverIntegrationEventTopics()
  {
    var entry = _assemblyAccessor.GetEntryAssembly();
    var companyPrefix = GetCompanyPrefix(entry);

    // AppDomain scan (because handlers can live across assemblies loaded into the host).
    var integrationEventTypes = AppDomain.CurrentDomain
      .GetAssemblies()
      .Where(a => a is not null && !a.IsDynamic)
      .Where(a => a.FullName is not null && a.FullName.StartsWith(companyPrefix, StringComparison.Ordinal))
      .SelectMany(a =>
      {
        try { return a.ExportedTypes; }
        catch { return Array.Empty<Type>(); }
      })
      .SelectMany(t => t.GetInterfaces())
      .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
      .SelectMany(i => i.GenericTypeArguments)
      .Where(t =>
        typeof(IIntegrationEvent).IsAssignableFrom(t) ||
        t.GetInterfaces().Any(x => x == typeof(IIntegrationEvent)))
      .Distinct()
      .ToList();

    // Map each integration event to a dedicated topic name (transport-level fanout).
    // Choose ONE naming strategy. I used ExchangeNamer because you already have it.
    var topics = new HashSet<string>(StringComparer.Ordinal);

    foreach (var evtType in integrationEventTypes)
    {
      // This is the "per-event topic" (replace with your TopicNamer if that's your canonical strategy)
      var perEventTopic = ExchangeNamer.GetEventExchangeName(evtType.Assembly);
      if (!string.IsNullOrWhiteSpace(perEventTopic))
        topics.Add(perEventTopic);
    }

    return topics.ToList();
  }

  private static string GetCompanyPrefix(IAssembly entry)
  {
    // Your old code did: entryAssembly.Name.Split(".").Take(1)
    // Keep it, but safe:
    var name = entry.Name ?? string.Empty;
    var first = name.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    return string.IsNullOrWhiteSpace(first) ? name : first;
  }
}
