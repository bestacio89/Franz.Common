using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages; // <- Franz INotification marker
using Franz.Common.Messaging.Configuration;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka
{
  public class MessagingInitializer : IMessagingInitializer
  {
    private static bool IsInitialized { get; set; }


    private readonly IAdminClient AdminClient;
    private readonly IAssemblyAccessor assemblyAccessor;
    private readonly string topicName;
    private readonly string deadLetterTopicName;

    public MessagingInitializer(IAdminClient adminClient, IAssemblyAccessor assemblyAccessor, IOptions<MessagingOptions> messagingOptions)
    {
      this.AdminClient = adminClient;
      this.assemblyAccessor = assemblyAccessor;
      var assembly = assemblyAccessor.GetEntryAssembly();
      topicName = TopicNamer.GetTopicName((System.Reflection.Assembly)assembly);
      deadLetterTopicName = TopicNamer.GetDeadLetterTopicName((System.Reflection.Assembly)assembly);
    }

    public void Initialize()
    {
      if (!IsInitialized)
      {
        InitializeTopic();
        InitializeDeadLetterTopic();
        InitializeTopicForSubscriptions();

        IsInitialized = true;
      }
    }

    private void InitializeTopic()
    {
      AdminClient.CreateTopicsAsync(new TopicSpecification[] {
            new TopicSpecification() { Name = topicName, ReplicationFactor = 1, NumPartitions = 1 }
      });
    }

    private void InitializeDeadLetterTopic()
    {
      AdminClient.CreateTopicsAsync(new TopicSpecification[] {
            new TopicSpecification() { Name = deadLetterTopicName, ReplicationFactor = 1, NumPartitions = 1 }
      });
    }

    private void InitializeTopicForSubscriptions()
    {
      var entryAssembly = assemblyAccessor.GetEntryAssembly();
      var companyName = string.Join(".", entryAssembly.Name!.Split(".").Take(1));

      AppDomain.CurrentDomain
          .GetAssemblies()
          .Where(assembly => !assembly.IsDynamic && assembly.FullName!.StartsWith(companyName))
          .SelectMany(assembly => assembly.ExportedTypes)
          .SelectMany(type => type.GetInterfaces())
          .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
          .SelectMany(contract => contract.GenericTypeArguments)
          .Where(type => type.GetInterfaces().Any(contract => contract.IsAssignableTo(typeof(IIntegrationEvent))))
          .ToList()
          .ForEach(integrationEventType =>
        {
          InitializeExchangeForSubscription(integrationEventType);
        });
    }

    private void InitializeExchangeForSubscription(Type integrationEventType)
    {
      var exchangeSourceName = ExchangeNamer.GetEventExchangeName(integrationEventType.Assembly);
      var classEventName = HeaderNamer.GetEventClassName(integrationEventType);

      AdminClient.CreateTopicsAsync(new TopicSpecification[] {
    new TopicSpecification() { Name = topicName, ReplicationFactor = 1, NumPartitions = 1 }
  });

      //AdminClient.CreatePartitionsAsync(
      //  topicName,
      //  new List<TopicPartitionAssignment>
      //  {
      //  new TopicPartitionAssignment()
      //  {
      //      Partition = 0,
      //      Replicas = new int[] { 0 },
      //      Topic = exchangeSourceName,
      //      CustomHeaders = new Dictionary<string, object>
      //      {
      //          { MessagingConstants.ClassName, classEventName },
      //          { "x-match", "all" }
      //      }
      //  }
      //  }
      //);
    }

  }
}
