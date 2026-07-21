namespace Franz.Common.Mediator.SourceGenerator.Constants;

internal static class MediatorKnownTypes
{
  public const string ICommandHandler = "Franz.Common.Mediator.Handlers.ICommandHandler<TRequest, TResponse>";
  public const string IQueryHandler = "Franz.Common.Mediator.Handlers.IQueryHandler<TQuery, TResponse>";
  public const string INotificationHandler = "Franz.Common.Mediator.Handlers.INotificationHandler<TNotification>";
  public const string IEventHandler = "Franz.Common.Mediator.Handlers.IEventHandler<TEvent>";
  public const string IStreamQueryHandler = "Franz.Common.Mediator.Handlers.IStreamQueryHandler<TQuery, TResponse>";
  public const string IValidator = "Franz.Common.Mediator.Validation.IValidator<TMessage>";
}