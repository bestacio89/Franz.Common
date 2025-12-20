using Azure.Messaging.ServiceBus;
using Franz.Common.Errors;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.AzureEventBus.Mapping;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.AzureEventBus.Consumers;

/// <summary>
/// Azure Service Bus message processor.
/// Pure transport consumer: maps to Franz Message and publishes to mediator.
/// </summary>
public sealed class AzureEventBusProcessor : IAsyncDisposable
{
  private readonly ServiceBusProcessor _processor;
  private readonly IAzureEventBusMessageMapper _mapper;
  private readonly IDispatcher _mediator;
  private readonly ILogger<AzureEventBusProcessor> _logger;
  public string EntityName { get; set; } = string.Empty;
  public AzureEventBusProcessor(
    ServiceBusClient client,
    string entityName,
    IAzureEventBusMessageMapper mapper,
    IDispatcher mediator,
    ILogger<AzureEventBusProcessor> logger,
    ServiceBusProcessorOptions options)
  {
    _mapper = mapper;
    _mediator = mediator;
    _logger = logger;

    _processor = client.CreateProcessor(entityName, options);

    _processor.ProcessMessageAsync += OnMessageAsync;
    _processor.ProcessErrorAsync += OnErrorAsync;
  }

  public async Task StartAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("🚀 Starting AzureEventBusProcessor");
    await _processor.StartProcessingAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("🛑 Stopping AzureEventBusProcessor");
    await _processor.StopProcessingAsync(cancellationToken);
  }

  private async Task OnMessageAsync(ProcessMessageEventArgs args)
  {
    Message franzMessage;

    try
    {
      franzMessage = _mapper.FromServiceBusMessage(args.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "🔥 Failed to map Azure Service Bus message. Dead-lettering.");
      await args.DeadLetterMessageAsync(args.Message, cancellationToken: args.CancellationToken);
      return;
    }

    try
    {
      // 🔥 THIS IS THE MAGIC BOUNDARY
      await _mediator.PublishNotificationAsync(franzMessage, args.CancellationToken);

      await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(
          ex,
          "⚠️ Error while processing message {MessageId}. Abandoning.",
          franzMessage.Id);

      await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
    }
  }

  private Task OnErrorAsync(ProcessErrorEventArgs args)
  {
    _logger.LogError(
        args.Exception,
        "🔥 Azure Service Bus processor error. Entity={EntityPath}, Source={ErrorSource}",
        args.EntityPath,
        args.ErrorSource);

    return Task.CompletedTask;
  }

  public async ValueTask DisposeAsync()
  {
    await _processor.DisposeAsync();
  }
}
