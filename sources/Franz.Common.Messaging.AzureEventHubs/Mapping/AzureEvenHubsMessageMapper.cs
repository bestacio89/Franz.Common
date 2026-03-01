#nullable enable
using Azure.Messaging.EventHubs.Processor;
using Franz.Common.Messaging.AzureEventHubs.Constants;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.AzureEventHubs.Mapping;

public sealed class AzureEventHubsMessageMapper
{
  public Message FromEvent(ProcessEventArgs args, string body)
  {
    var data = args.Data;


    // The Message constructor handles the default Guid v7 for Id and CorrelationId.
    var message = new Message(body);

    // Attempt to parse the native EventData MessageId (string) into our native Guid
    if (!string.IsNullOrWhiteSpace(data.MessageId) && Guid.TryParse(data.MessageId, out var messageGuid))
    {
      message.Id = messageGuid;
    }

    // Attempt to parse the native EventData CorrelationId (string) into our native Guid
    if (!string.IsNullOrWhiteSpace(data.CorrelationId) && Guid.TryParse(data.CorrelationId, out var correlationGuid))
    {
      message.CorrelationId = correlationGuid;
    }

    // Event Hubs → Franz transport headers
    message.Headers[AzureEventHubsHeaders.PartitionId] =
        new StringValues(args.Partition.PartitionId);

    message.Headers[AzureEventHubsHeaders.SequenceNumber] =
        new StringValues(data.SequenceNumber.ToString());

    message.Headers[AzureEventHubsHeaders.Offset] =
        new StringValues(data.Offset.ToString());

    message.Headers[AzureEventHubsHeaders.EnqueuedTime] =
        new StringValues(data.EnqueuedTime.ToString("O"));

    return message;
  }
}