using Franz.Common.Messaging.Contexting;

public class MessageContextAccessor : IMessageContextAccessor
{
  private static readonly AsyncLocal<IMessageContext?> current = new();

  public IMessageContext? Current => current.Value;

  public void Set(IMessageContext context) => current.Value = context;
  public void Clear() => current.Value = null;
}
