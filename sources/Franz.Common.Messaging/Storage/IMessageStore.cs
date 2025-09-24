using Franz.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Franz.Common.Messaging.Storage;

public interface IMessageStore
{
  Task SaveAsync(Message message, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default);
  Task MarkAsSentAsync(string messageId, CancellationToken cancellationToken = default);
}
