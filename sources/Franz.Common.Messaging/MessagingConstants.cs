namespace Franz.Common.Messaging;

public static class MessagingConstants
{
    public static string MessageId { get; } = nameof(MessageId);

    public static string ClassName { get; } = nameof(ClassName);

    public static string FaultCode { get; } = nameof(FaultCode);

    /// <summary>Identifies a transport-generated fallback ID that is not stable across redelivery.</summary>
    public static string TransportGeneratedMessageId { get; } = nameof(TransportGeneratedMessageId);
}
