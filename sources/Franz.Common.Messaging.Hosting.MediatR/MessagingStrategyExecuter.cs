using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Hosting.Delegating;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Messages;
using Newtonsoft.Json;


namespace Franz.Common.Messaging.Hosting;

public class MessagingStrategyExecuter : IMessagingStrategyExecuter
{
  private readonly IEnumerator<IAsyncMessageActionFilter> messagingFilters;
  private readonly IDispatcher dispatcher;

  private MessageActionExecutingContext messageActionExecutingContext = null!;
  private MessageActionExecutedContext messageActionExecutedContext = null!;
  private Func<Task> action = null!;

  public MessagingStrategyExecuter(IEnumerable<IAsyncMessageActionFilter> messagingFilters, IDispatcher dispatcher)
  {
    this.messagingFilters = messagingFilters.GetEnumerator();
    this.dispatcher = dispatcher;
  }

  public Task<bool> CanExecuteAsync(Message message)
  {
    var result = message.Headers.ContainsKey(MessagingConstants.ClassName);
    return Task.FromResult(result);
  }

  public async Task ExecuteAsync(Message message)
  {
    messageActionExecutingContext = new MessageActionExecutingContext(message);
    messageActionExecutedContext = new MessageActionExecutedContext(message);

    action = async () =>
    {
      await Process(message);
    };

    await Next().Invoke();

    if (messageActionExecutedContext.Exception != null)
      throw messageActionExecutedContext.Exception;
  }

  private MessageActionExecutionDelegate Next()
  {
    return new MessageActionExecutionDelegate(async () =>
    {
      try
      {
        if (messagingFilters.MoveNext())
          await messagingFilters.Current.OnActionExecutionAsync(messageActionExecutingContext, Next());
        else
          await action.Invoke();
      }
      catch (Exception ex)
      {
        messageActionExecutedContext.Exception = ex.GetBaseException();
      }

      return messageActionExecutedContext;
    });
  }

  private async Task Process(Message message)
  {
    var classMessage = GetParameter(message);

    switch (classMessage)
    {
      case IIntegrationEvent integrationEvent:
        await dispatcher.PublishNotificationAsync(integrationEvent);
        break;

      case ICommand command:
        await dispatcher.SendAsync(command);
        break;

      case IQuery<object> query:
        await dispatcher.SendAsync(query);
        break;

      default:
        throw new TechnicalException(
            $"Unsupported message type. Must be {nameof(IIntegrationEvent)}, {nameof(ICommand)}, or {nameof(IQuery<object>)}");
    }
  }

  private static object GetParameter(Message message)
  {
    var parameterType = GetParameterType(message);

    var json = message.Body ?? throw new TechnicalException("Message body is null");
    var result = JsonConvert.DeserializeObject(json, parameterType)
                 ?? throw new TechnicalException("Failed to deserialize message body");

    return result;
  }

  private static Type GetParameterType(Message message)
  {
    var className = message.Headers.GetClassName();
    var result = Type.GetType(className)
                 ?? throw new TechnicalException($"Message type '{className}' could not be resolved");

    return result;
  }
}
