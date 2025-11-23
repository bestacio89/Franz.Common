namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public interface IRabbitMqMessageModel
{
  Task ProduceAsync<TMessage>(
      string exchange,
      string routingKey,
      TMessage message,
      CancellationToken cancellationToken = default);
}
