using System;
using System.Collections.Generic;
using System.Text;
using static MongoDB.Driver.WriteConcern;
using Franz.Common.Messaging.RabbitMQ.Modeling;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Abstractions;

public interface IQueueProvisioner
{
  void EnsureQueueExists(IModelProvider channel, string queueName);
}