using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Hosting.Listeners;

public interface IMessageListener
{
  Task StartAsync(CancellationToken cancellationToken);
  Task StopAsync(CancellationToken cancellationToken);
}
