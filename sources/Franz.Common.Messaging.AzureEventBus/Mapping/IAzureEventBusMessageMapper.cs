using Azure.Messaging.ServiceBus;
using Franz.Common.Messaging;

namespace Franz.Common.Messaging.AzureEventBus.Mapping;

/// <summary>
/// Maps between Franz transport messages and Azure Service Bus messages.
/// Pure transport adapter – no domain logic.
/// </summary>
public interface IAzureEventBusMessageMapper
{
  ServiceBusMessage ToServiceBusMessage(Message message);

  Message FromServiceBusMessage(ServiceBusReceivedMessage message);
}
