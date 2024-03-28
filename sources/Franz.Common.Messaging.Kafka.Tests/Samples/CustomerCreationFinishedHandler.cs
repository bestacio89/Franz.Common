using MediatR;

namespace Franz.Common.Messaging.Kafka.Tests.Samples;
public class CustomerCreationFinishedHandler : INotificationHandler<CustomerCreationFinished>
{
  public Task Handle(CustomerCreationFinished notification, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
