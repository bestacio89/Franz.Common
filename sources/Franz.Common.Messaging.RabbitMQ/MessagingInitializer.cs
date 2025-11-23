using Franz.Common.Business.Events;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Franz.Common.Mediator;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class MessagingInitializer : IMessagingInitializer
{
  private static bool IsInitialized = false;

  private readonly IModelProvider modelProvider;
  private readonly IAssemblyAccessor assemblyAccessor;
  private readonly string exchangeName;
  private readonly string queueName;
  private readonly string deadLetterQueueName;
  private readonly string deadLetterExchangeName;

  public MessagingInitializer(
      IModelProvider modelProvider,
      IAssemblyAccessor assemblyAccessor,
      IOptions<MessagingOptions> options)
  {
    this.modelProvider = modelProvider;
    this.assemblyAccessor = assemblyAccessor;

    var assembly = assemblyAccessor.GetEntryAssembly();

    exchangeName = ExchangeNamer.GetEventExchangeName(assembly);
    queueName = QueueNamer.GetQueueName(assembly);
    deadLetterQueueName = QueueNamer.GetDeadLetterQueueName(assembly);
    deadLetterExchangeName = ExchangeNamer.GetDeadLetterExchangeName(assembly);
  }

  public void Initialize()
  {
    if (IsInitialized)
      return;

    InitializeExchange();
    InitializeQueue();
    InitializeDeadLetterQueue();
    InitializeExchangesForSubscriptions();

    IsInitialized = true;
  }

  private void InitializeExchange()
  {
    modelProvider.Current.ExchangeDeclareAsync(
        exchangeName,
        ExchangeType.Headers,
        durable: true,
        autoDelete: false);
  }

  private void InitializeQueue()
  {
    modelProvider.Current.QueueDeclareAsync(
        queue: queueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: new Dictionary<string, object>
        {
                { "x-queue-type", "quorum" },
                { "x-dead-letter-exchange", deadLetterExchangeName },
                { "x-dead-letter-routing-key", deadLetterQueueName },
        });
  }

  private void InitializeDeadLetterQueue()
  {
    modelProvider.Current.ExchangeDeclareAsync(
        exchange: deadLetterExchangeName,
        type: ExchangeType.Direct,
        durable: true,
        autoDelete: false);

    modelProvider.Current.QueueDeclareAsync(
        queue: deadLetterQueueName,
        durable: true,
        exclusive: false,
        autoDelete: false);

    modelProvider.Current.QueueBindAsync(
        queue: deadLetterQueueName,
        exchange: deadLetterExchangeName,
        routingKey: deadLetterQueueName);
  }

  private void InitializeExchangesForSubscriptions()
  {
    var entryAssembly = assemblyAccessor.GetEntryAssembly();
    var companyName = string.Join(".", entryAssembly.Name!.Split(".").Take(1));

    var integrationEvents =
        AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a => !a.IsDynamic && a.FullName!.StartsWith(companyName))
        .SelectMany(a => a.ExportedTypes)
        .Where(t => t.GetInterfaces().Any(ifc =>
            ifc.IsGenericType &&
            ifc.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
        .SelectMany(type => type.GetInterfaces())
        .Where(ifc => ifc.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
        .Select(ifc => ifc.GetGenericArguments()[0])
        .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t))
        .Distinct();

    foreach (var eventType in integrationEvents)
    {
      InitializeExchangeForSubscription(eventType);
    }
  }

  private void InitializeExchangeForSubscription(Type integrationEventType)
  {
    var sourceExchange = ExchangeNamer.GetEventExchangeName(integrationEventType.Assembly);
    var eventName = HeaderNamer.GetEventClassName(integrationEventType);

    modelProvider.Current.QueueBindAsync(
        queue: queueName,
        exchange: sourceExchange,
        routingKey: "",
        arguments: new Dictionary<string, object>
        {
                { MessagingConstants.ClassName, eventName },
                { "x-match", "all" }
        });
  }
}
