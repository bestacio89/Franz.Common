using Azure.Messaging.EventHubs.Processor;
using Franz.Common.Messaging;
using Franz.Common.Messaging.AzureEventHubs.Constants;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.AzureEventHubs.Mapping;

public sealed class AzureEventHubsMessageMapper
{
  public Message FromEvent(ProcessEventArgs args, string body)
  {
    var data = args.Data;

    var message = new Message
    {
      Id = data.MessageId ?? Guid.NewGuid().ToString("N"),
      CorrelationId = data.CorrelationId ?? string.Empty,
      Body = body
    };

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
