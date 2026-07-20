#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Observers;

public class ConsoleMediatorObserver : IMediatorObserver
{
  private readonly ConsoleObserverOptions _options;

  public ConsoleMediatorObserver(ConsoleObserverOptions options)
  {
    _options = options ?? new ConsoleObserverOptions();
  }

  private void Write(string message, ConsoleColor color)
  {
    if (_options.UseColors)
    {
      Console.ForegroundColor = color;
      Console.WriteLine(message);
      Console.ResetColor();
    }
    else
    {
      Console.WriteLine(message);
    }
  }

  public Task OnRequestStarted(object request, Guid correlationId, CancellationToken cancellationToken)
  {
    var context = MediatorContext.Current;
    var typeName = _options.ShowFullTypeName ? request.GetType().FullName : request.GetType().Name;

    Write($"[Mediator] START → {typeName} | CorrelationId={context.CorrelationId} | User={context.UserId} | Tenant={context.TenantId}",
          ConsoleColor.Cyan);

    return Task.CompletedTask;
  }

  public Task OnRequestCompleted(object request, object? response, Guid correlationId, TimeSpan duration, CancellationToken cancellationToken)
  {
    var context = MediatorContext.Current;
    var typeName = _options.ShowFullTypeName ? request.GetType().FullName : request.GetType().Name;

    Write($"[Mediator] SUCCESS → {typeName} | CorrelationId={context.CorrelationId} | Duration={duration.TotalMilliseconds:N0}ms",
          ConsoleColor.Green);

    if (_options.ShowResponse && response is not null)
    {
      Write($"    Response: {response}", ConsoleColor.Green);
    }

    return Task.CompletedTask;
  }

  public Task OnRequestFailed(object request, Exception exception, Guid correlationId, TimeSpan duration, CancellationToken cancellationToken)
  {
    var context = MediatorContext.Current;
    var typeName = _options.ShowFullTypeName ? request.GetType().FullName : request.GetType().Name;

    Write($"[Mediator] FAIL → {typeName} | CorrelationId={context.CorrelationId} | Duration={duration.TotalMilliseconds:N0}ms",
          ConsoleColor.Red);
    Write($"    Exception: {exception.Message}", ConsoleColor.Red);

    if (_options.ShowStackTrace)
    {
      Write(exception.StackTrace ?? "<no stack trace>", ConsoleColor.DarkRed);
    }

    return Task.CompletedTask;
  }

  // Notification handlers
  public Task OnNotificationHandlerStarted(object notification, Type handlerType, Guid correlationId, CancellationToken cancellationToken)
  {
    if (!_options.ShowNotificationHandlers) return Task.CompletedTask;

    var context = MediatorContext.Current;
    Write($"[Mediator] HANDLER START → {handlerType.Name} for {notification.GetType().Name} | CorrelationId={context.CorrelationId}",
          ConsoleColor.DarkCyan);

    return Task.CompletedTask;
  }

  public Task OnNotificationHandlerCompleted(object notification, Type handlerType, Guid correlationId, TimeSpan duration, CancellationToken cancellationToken)
  {
    if (!_options.ShowNotificationHandlers) return Task.CompletedTask;

    Write($"[Mediator] HANDLER SUCCESS → {handlerType.Name} | Duration={duration.TotalMilliseconds:N0}ms",
          ConsoleColor.DarkGreen);

    return Task.CompletedTask;
  }

  public Task OnNotificationHandlerFailed(object notification, Type handlerType, Guid correlationId, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
  {
    if (!_options.ShowNotificationHandlers) return Task.CompletedTask;

    Write($"[Mediator] HANDLER FAIL → {handlerType.Name} | Duration={duration.TotalMilliseconds:N0}ms",
          ConsoleColor.DarkRed);
    Write($"    Exception: {exception.Message}", ConsoleColor.DarkRed);

    return Task.CompletedTask;
  }
}