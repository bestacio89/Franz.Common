namespace Franz.Common.Messaging.Contexting;

public interface IMessageContextAccessor
{

  IMessageContext? Current { get; }

}
