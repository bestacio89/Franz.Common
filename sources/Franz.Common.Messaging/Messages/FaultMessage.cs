namespace Franz.Common.Messaging.Messages;

public sealed class ExecutionFault : IExecutionFault
{
  public string Code { get; init; }
  public string Message { get; init; }
  public string? Source { get; init; }
  public string? StackTrace { get; init; }
  public DateTimeOffset OccurredAt { get; init; }

  public ExecutionFault(
      string code,
      string message,
      string? source = null,
      string? stackTrace = null)
  {
    Code = code;
    Message = message;
    Source = source;
    StackTrace = stackTrace;
    OccurredAt = DateTimeOffset.UtcNow;
  }

  public static ExecutionFault FromException(Exception ex)
  {
    return new ExecutionFault(
        code: ex.GetType().Name,
        message: ex.Message,
        source: ex.Source,
        stackTrace: ex.StackTrace);
  }
}
