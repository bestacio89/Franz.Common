namespace Franz.Common.Messaging;

public static class MessagingConstants
{
    public static string MessageId { get; } = nameof(MessageId);

    public static string ClassName { get; } = nameof(ClassName);

    public static string FaultCode { get; } = nameof(FaultCode);

    /// <summary>Message identity was synthesized because the inbound transport omitted a valid identity.</summary>
    public static string TransportGeneratedMessageId { get; } = nameof(TransportGeneratedMessageId);
}
