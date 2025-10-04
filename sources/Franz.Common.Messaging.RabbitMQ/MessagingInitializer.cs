using Franz.Common.Business.Events;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Franz.Common.Mediator;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.Messaging.RabbitMQ;

public class MessagingInitializer : IMessagingInitializer
{
  private static bool IsInitialized { get; set; }

  private readonly IModelProvider modelProvider;
  private readonly IAssemblyAccessor assemblyAccessor;
  private readonly string exchangeName;
  private readonly string queueName;
  private readonly string deadLetterQueueName;
  private readonly string deadLetterExchangeName;

  public MessagingInitializer(IModelProvider modelProvider, IAssemblyAccessor assemblyAccessor, IOptions<MessagingOptions> messagingOptions)
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
    if (!IsInitialized)
    {
      InitializeExchange();
      InitializeQueue();
      InitializeDeadLetterQueue();
      InitializeExchangesForSubscriptions();

      IsInitialized = true;
    }
  }

  private void InitializeExchange()
  {
    modelProvider.Current.ExchangeDeclare(exchangeName, ExchangeType.Headers, true, false, null);
  }

  private void InitializeQueue()
  {
    modelProvider.Current.QueueDeclare(queueName, true, false, false, new Dictionary<string, object>
      {
        { "x-queue-type", "quorum" },
        { "x-dead-letter-exchange", deadLetterExchangeName },
        { "x-dead-letter-routing-key", deadLetterQueueName },
      });
  }

  private void InitializeDeadLetterQueue()
  {
    modelProvider.Current.ExchangeDeclare(deadLetterExchangeName, ExchangeType.Direct, true, false, new Dictionary<string, object> { { "x-queue-type", "quorum" } });
    modelProvider.Current.QueueDeclare(deadLetterQueueName, true, false, false, null);
    modelProvider.Current.QueueBind(deadLetterQueueName, deadLetterExchangeName, deadLetterQueueName, null);
  }

  private void InitializeExchangesForSubscriptions()
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

    modelProvider.Current.QueueBind(queueName, exchangeSourceName, string.Empty, new Dictionary<string, object>
      {
          { MessagingConstants.ClassName, classEventName },
          { "x-match", "all" },
      });
  }
}
