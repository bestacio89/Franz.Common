namespace Franz.Common.Mediator.Diagnostics
{
  public interface IMediatorObserver
  {
    Task OnRequestStarted(object request, string correlationId, CancellationToken cancellationToken);
    Task OnRequestCompleted(object request, object? response, string correlationId, TimeSpan duration, CancellationToken cancellationToken);
    Task OnRequestFailed(object request, Exception exception, string correlationId, TimeSpan duration, CancellationToken cancellationToken);

    // New for notification handlers
    Task OnNotificationHandlerStarted(object notification, Type handlerType, string correlationId, CancellationToken cancellationToken);
    Task OnNotificationHandlerCompleted(object notification, Type handlerType, string correlationId, TimeSpan duration, CancellationToken cancellationToken);
    Task OnNotificationHandlerFailed(object notification, Type handlerType, string correlationId, Exception exception, TimeSpan duration, CancellationToken cancellationToken);
  }
}
