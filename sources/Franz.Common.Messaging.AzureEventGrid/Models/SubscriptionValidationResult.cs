namespace Franz.Common.Messaging.AzureEventGrid.Models;

/// <summary>
/// Result returned when an Event Grid subscription validation event is detected.
/// </summary>
public sealed record SubscriptionValidationResult(string ValidationCode);
