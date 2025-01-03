using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.MassTransit.Clients;
public interface IMessagingClient
{
  Task PublishAsync<T>(T message, string topic, CancellationToken cancellationToken = default);
  Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken = default);
}
