#nullable enable
namespace Franz.Common.Messaging.Exceptions;

/// <summary>
/// Identifies a deterministic message failure that cannot succeed by retrying
/// the same payload and configuration. Outbox publishers dead-letter these
/// messages immediately, independently of the transient retry dead-letter option.
/// </summary>
public sealed class PermanentMessagingException : Exception
{
  public PermanentMessagingException(string message)
      : base(message)
  {
  }

  public PermanentMessagingException(string message, Exception innerException)
      : base(message, innerException)
  {
  }
}