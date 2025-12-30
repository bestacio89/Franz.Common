using System;
using System.Collections.Generic;
using System.Text;
using static MongoDB.Driver.WriteConcern;
using Franz.Common.Messaging.RabbitMQ.Modeling;
namespace Franz.Common.Messaging.Hosting.RabbitMQ.Abstractions;

public sealed class DefaultQueueProvisioner : IQueueProvisioner
{
  public void EnsureQueueExists(IModelProvider channel, string queueName)
  {
    // Idempotent in RabbitMQ if parameters match
    channel.Current.QueueDeclareAsync(
      queue: queueName,
      durable: true,
      exclusive: false,
      autoDelete: false,
      arguments: null);
  }
}