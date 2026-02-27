namespace Franz.Common.Mediator.Diagnostics
{
  public interface IMediatorObserver
  {
    Task OnRequestStarted(object request, Guid correlationId, CancellationToken cancellationToken);
    Task OnRequestCompleted(object request, object? response, Guid correlationId, TimeSpan duration, CancellationToken cancellationToken);
    Task OnRequestFailed(object request, Exception exception, Guid correlationId, TimeSpan duration, CancellationToken cancellationToken);

    // New for notification handlers
    Task OnNotificationHandlerStarted(object notification, Type handlerType, Guid correlationId, CancellationToken cancellationToken);
    Task OnNotificationHandlerCompleted(object notification, Type handlerType, Guid correlationId, TimeSpan duration, CancellationToken cancellationToken);
    Task OnNotificationHandlerFailed(object notification, Type handlerType, Guid correlationId, Exception exception, TimeSpan duration, CancellationToken cancellationToken);
  }
}
