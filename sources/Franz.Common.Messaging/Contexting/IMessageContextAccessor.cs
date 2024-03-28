namespace Franz.Common.Messaging.Contexting;

public interface IMessageContextAccessor
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  IMessageContext? Current { get; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}
