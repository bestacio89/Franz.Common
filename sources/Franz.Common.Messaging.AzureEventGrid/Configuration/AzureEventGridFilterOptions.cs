namespace Franz.Common.Messaging.AzureEventGrid.Configuration;

public sealed class AzureEventGridFilterOptions
{
  public ISet<string> AllowedEventTypes { get; } =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase);

  public bool IsAllowed(string eventType)
      => AllowedEventTypes.Count == 0 || AllowedEventTypes.Contains(eventType);
}
