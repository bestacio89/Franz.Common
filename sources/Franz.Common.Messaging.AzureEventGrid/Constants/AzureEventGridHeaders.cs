namespace Franz.Common.Messaging.AzureEventGrid.Constants;

/// <summary>
/// Header keys used when mapping Azure Event Grid metadata into Franz messages.
/// </summary>
public static class AzureEventGridHeaders
{
  public const string EventType = "franz-eventgrid-event-type";
  public const string Subject = "franz-eventgrid-subject";
  public const string Topic = "franz-eventgrid-topic";
  public const string EventTime = "franz-eventgrid-event-time";
  public const string DataVersion = "franz-eventgrid-data-version";
}
