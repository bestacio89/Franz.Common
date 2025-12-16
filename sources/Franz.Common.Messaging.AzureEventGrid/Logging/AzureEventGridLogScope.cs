using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.AzureEventGrid.Logging;

internal static class AzureEventGridLogScope
{
  public static IDisposable BeginScope(
      this ILogger logger,
      EventGridEvent evt)
  {
    return logger.BeginScope(new Dictionary<string, object?>
    {
      ["EventGrid.EventId"] = evt.Id,
      ["EventGrid.EventType"] = evt.EventType,
      ["EventGrid.Subject"] = evt.Subject,
      ["EventGrid.Topic"] = evt.Topic
    });
  }
}
