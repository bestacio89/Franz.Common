namespace Franz.Common.Messaging.Messages;

public interface IExecutionFault : ISystemMessage
{
  string Code { get; }
  string Message { get; }
  string? Source { get; }
  string? StackTrace { get; }
  DateTimeOffset OccurredAt { get; }
}
