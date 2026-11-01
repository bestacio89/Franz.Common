namespace Franz.Common.Mediator.SourceGenerator.Constants;

internal static class MediatorKnownTypes
{
  public const string ICommandHandler = "Franz.Common.Mediator.Handlers.ICommandHandler<TCommand, TResponse>";
  public const string IVoidCommandHandler = "Franz.Common.Mediator.Handlers.ICommandHandler<TCommand>";
  public const string IQueryHandler = "Franz.Common.Mediator.Handlers.IQueryHandler<TQuery, TResponse>";
  public const string INotificationHandler = "Franz.Common.Mediator.Messages.INotificationHandler<TNotification>";
  public const string IEventHandler = "Franz.Common.Mediator.Handlers.IEventHandler<TEvent>";
  public const string IStreamQueryHandler = "Franz.Common.Mediator.Messages.IStreamQueryHandler<TQuery, TResponse>";
  public const string IValidator = "Franz.Common.Mediator.Pipelines.Validation.IValidator<TRequest>";
}
