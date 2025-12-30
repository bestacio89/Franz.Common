using Franz.Common.Messaging.RabbitMQ.Modeling;

public interface IQueueProvisioner
{
  Task EnsureQueueExistsAsync(
    IModelProvider modelProvider,
    string queueName,
    CancellationToken ct = default);
}
