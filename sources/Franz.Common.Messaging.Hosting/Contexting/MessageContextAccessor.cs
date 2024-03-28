using Franz.Common.Messaging.Contexting;

namespace Franz.Common.Messaging.Hosting.Contexting;

public class MessageContextAccessor : IMessageContextAccessor
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static AsyncLocal<IMessageContext?> current = new();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public IMessageContext? Current { get => current?.Value; internal set => current.Value = value; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}
