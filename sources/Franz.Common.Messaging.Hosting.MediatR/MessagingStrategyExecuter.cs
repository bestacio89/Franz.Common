using Franz.Common.Business.Commands;
using Franz.Common.Business.Events;

using Franz.Common.Errors;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Hosting.Delegating;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.MediatR.Properties;

using MediatR;

using Newtonsoft.Json;

namespace Franz.Common.Messaging.Hosting.MediatR;

public class MessagingStrategyExecuter : IMessagingStrategyExecuter
{
  private readonly IEnumerator<IAsyncMessageActionFilter> messagingFilters;
  private readonly IMediator mediator;

  private MessageActionExecutingContext messageActionExecutingContext = null!;
  private MessageActionExecutedContext messageActionExecutedContext = null!;
  private Func<Task> action = null!;

  public MessagingStrategyExecuter(IEnumerable<IAsyncMessageActionFilter> messagingFilters, IMediator mediator)
  {
    this.messagingFilters = messagingFilters.GetEnumerator();
    this.mediator = mediator;
  }

  public Task<bool> CanExecuteAsync(Message message)
  {
    var result = message.Headers.ContainsKey(MessagingConstants.ClassName);

    return Task.FromResult(result);
  }

  public async Task ExecuteEsync(Message message)
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

    if (classMessage is IIntegrationEvent integrationEvent)
      await mediator.Publish(integrationEvent);
    else if (classMessage is ICommandBaseRequest commandBaseRequest)
      await mediator.Send(commandBaseRequest);
    else
      throw new TechnicalException(string.Format(Resources.MessagingStrategyExecuterParameterNotImplementedException, $"{nameof(IIntegrationEvent)} or {nameof(ICommandBaseRequest)}"));
  }

  private static object GetParameter(Message message)
  {
    var parameterType = GetParameterType(message);

    var json = message.Body ?? throw new TechnicalException(Resources.MessagingStrategyExecuterParameterNullException);
    var result = JsonConvert.DeserializeObject(json, parameterType) ?? throw new TechnicalException(Resources.MessagingStrategyExecuterParameterDeserializationException);

    return result;
  }

  private static Type GetParameterType(Message message)
  {
    var className = message.Headers.GetClassName();
    var result = Type.GetType(className) ?? throw new TechnicalException(Resources.MessagingStrategyExecuterParameterException);

    return result;
  }
}
