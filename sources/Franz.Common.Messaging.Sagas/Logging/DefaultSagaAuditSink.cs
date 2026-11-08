#nullable enable

using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Sagas.Logging;

/// <summary>
/// Default metadata-only audit sink backed by structured application logging.
/// This is observability output, not a durable compliance audit store.
/// </summary>
internal sealed class DefaultSagaAuditSink : ISagaAuditSink
{
  private readonly ILogger<DefaultSagaAuditSink> _logger;

  public DefaultSagaAuditSink(ILogger<DefaultSagaAuditSink> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    cancellationToken.ThrowIfCancellationRequested();

    _logger.LogInformation(
        "Saga transition | SagaId={SagaId} | SagaType={SagaType} | Step={StepType} | Status={Status} | Incoming={IncomingMessageType} | Outgoing={OutgoingMessageType} | CorrelationId={CorrelationId} | CausationId={CausationId} | MessageId={MessageId} | TenantId={TenantId} | DurationMs={DurationMs} | ErrorType={ErrorType}",
        record.SagaId,
        record.SagaType,
        record.StepType,
        record.Status,
        record.IncomingMessageType,
        record.OutgoingMessageType,
        record.CorrelationId,
        record.CausationId,
        record.MessageId,
        record.TenantId,
        record.Duration.TotalMilliseconds,
        record.ErrorType);

    return Task.CompletedTask;
  }
}
