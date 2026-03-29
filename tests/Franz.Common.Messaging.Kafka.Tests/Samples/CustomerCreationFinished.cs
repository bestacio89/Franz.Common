using Franz.Common.Business.Events;
using Franz.Common.Mediator;

namespace Franz.Common.Messaging.Kafka.Tests.Samples;
public class CustomerCreationFinished : IIntegrationEvent
{
  public long Id { get; set; }
}
