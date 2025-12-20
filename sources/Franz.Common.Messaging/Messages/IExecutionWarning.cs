namespace Franz.Common.Messaging.Messages;

public interface IExecutionWarning : ISystemMessage
{
  string Code { get; }
  string Message { get; }
}
