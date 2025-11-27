#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Logging;

/// <summary>
/// A pluggable sink for receiving saga audit records.
/// </summary>
public interface ISagaAuditSink
{
  Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default);
}
