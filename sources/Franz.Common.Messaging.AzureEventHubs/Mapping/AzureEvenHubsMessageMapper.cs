#nullable enable

using Azure.Messaging.EventHubs.Processor;
using Franz.Common.Messaging.AzureEventHubs.Constants;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.AzureEventHubs.Mapping;

public sealed class AzureEventHubsMessageMapper
{
  public Message FromEvent(ProcessEventArgs args, string body)
  {
    var data = args.Data;

    var message = new Message(body)
    {
      Id = RequireGuid(data.MessageId),
      CorrelationId = RequireGuid(data.CorrelationId)
    };

    ApplyHeaders(message, args, data);

    return message;
  }

  // -----------------------------
  // Headers
  // -----------------------------
  private static void ApplyHeaders(
    Message message,
    ProcessEventArgs args,
    Azure.Messaging.EventHubs.EventData data)
  {
    var headers = message.Headers;

    headers[AzureEventHubsHeaders.PartitionId] =
      new[] { args.Partition.PartitionId };

    headers[AzureEventHubsHeaders.SequenceNumber] =
      new[] { data.SequenceNumber.ToString() };

    // FIX: obsolete API → OffsetString
    headers[AzureEventHubsHeaders.Offset] =
      new[] { data.OffsetString };

    headers[AzureEventHubsHeaders.EnqueuedTime] =
      new[] { data.EnqueuedTime.ToString("O") };
  }

  // -----------------------------
  // Strict GUID parsing
  // -----------------------------
  private static Guid RequireGuid(string? value)
  {
    if (Guid.TryParse(value, out var guid))
      return guid;

    throw new InvalidOperationException($"Invalid GUID: '{value}'");
  }
}