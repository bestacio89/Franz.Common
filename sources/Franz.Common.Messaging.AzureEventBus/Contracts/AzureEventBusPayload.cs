namespace Franz.Common.Messaging.AzureEventBus.Contracts;

/// <summary>
/// Transport payload for Azure Service Bus.
/// This is NOT Azure SDK–specific.
/// </summary>
public sealed record AzureEventBusPayload(
    string EventType,
    object Data
);
