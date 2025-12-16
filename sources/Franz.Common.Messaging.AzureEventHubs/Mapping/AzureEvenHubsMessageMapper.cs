using Azure.Messaging.EventHubs.Processor;
using Franz.Common.Messaging;
using Franz.Common.Messaging.AzureEventHubs.Constants;

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

    message.Headers[AzureEventHubsHeaders.PartitionId] =
        args.Partition.PartitionId;

    message.Headers[AzureEventHubsHeaders.SequenceNumber] =
        data.SequenceNumber.ToString();

    message.Headers[AzureEventHubsHeaders.Offset] =
        data.Offset.ToString();

    message.Headers[AzureEventHubsHeaders.EnqueuedTime] =
        data.EnqueuedTime.ToString("O");

    return message;
  }
}
