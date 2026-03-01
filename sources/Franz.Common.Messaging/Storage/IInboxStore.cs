using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Storage;
public interface IInboxStore
{
  Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default);
  Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default);
}
