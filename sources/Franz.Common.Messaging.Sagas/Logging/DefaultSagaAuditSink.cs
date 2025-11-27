#nullable enable

using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Logging;

/// <summary>
/// Default audit sink that writes saga audit events to ILogger.
/// Lightweight, dependency-free, and safe for production.
/// </summary>
internal sealed class DefaultSagaAuditSink : ISagaAuditSink
{
  private readonly ILogger<DefaultSagaAuditSink> _logger;

  public DefaultSagaAuditSink(ILogger<DefaultSagaAuditSink> logger)
  {
    _logger = logger;
  }

  public Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation(
        "Saga Audit | {SagaType} [{SagaId}] | Step={StepType} | In={Incoming} | Out={Outgoing} | Duration={Duration}ms",
        record.SagaType,
        record.SagaId,
        record.StepType,
        record.IncomingMessageType,
        record.OutgoingMessageType,
        record.Duration.TotalMilliseconds
    );

    return Task.CompletedTask;
  }
}
