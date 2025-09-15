using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Observers
{
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

    public Task OnRequestStarted(object request, string correlationId, CancellationToken cancellationToken)
    {
      var typeName = _options.ShowFullTypeName ? request.GetType().FullName : request.GetType().Name;
      Write($"[Mediator] START → {typeName} | CorrelationId={correlationId}", ConsoleColor.Cyan);
      return Task.CompletedTask;
    }

    public Task OnRequestCompleted(object request, object? response, string correlationId, TimeSpan duration, CancellationToken cancellationToken)
    {
      var typeName = _options.ShowFullTypeName ? request.GetType().FullName : request.GetType().Name;
      Write($"[Mediator] SUCCESS → {typeName} | CorrelationId={correlationId} | Duration={duration.TotalMilliseconds:N0}ms", ConsoleColor.Green);

      if (_options.ShowResponse && response is not null)
      {
        Write($"    Response: {response}", ConsoleColor.Green);
      }

      return Task.CompletedTask;
    }

    public Task OnRequestFailed(object request, Exception exception, string correlationId, TimeSpan duration, CancellationToken cancellationToken)
    {
      var typeName = _options.ShowFullTypeName ? request.GetType().FullName : request.GetType().Name;
      Write($"[Mediator] FAIL → {typeName} | CorrelationId={correlationId} | Duration={duration.TotalMilliseconds:N0}ms", ConsoleColor.Red);
      Write($"    Exception: {exception.Message}", ConsoleColor.Red);

      if (_options.ShowStackTrace)
      {
        Write(exception.StackTrace ?? "<no stack trace>", ConsoleColor.DarkRed);
      }

      return Task.CompletedTask;
    }

    // OPTIONAL: hook into per-notification handlers
    public Task OnNotificationHandlerStarted(object notification, Type handlerType, string correlationId, CancellationToken cancellationToken)
    {
      if (!_options.ShowNotificationHandlers) return Task.CompletedTask;
      Write($"[Mediator] HANDLER START → {handlerType.Name} for {notification.GetType().Name} | CorrelationId={correlationId}", ConsoleColor.DarkCyan);
      return Task.CompletedTask;
    }

    public Task OnNotificationHandlerCompleted(object notification, Type handlerType, string correlationId, TimeSpan duration, CancellationToken cancellationToken)
    {
      if (!_options.ShowNotificationHandlers) return Task.CompletedTask;
      Write($"[Mediator] HANDLER SUCCESS → {handlerType.Name} | Duration={duration.TotalMilliseconds:N0}ms", ConsoleColor.DarkGreen);
      return Task.CompletedTask;
    }

    public Task OnNotificationHandlerFailed(object notification, Type handlerType, string correlationId, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
    {
      if (!_options.ShowNotificationHandlers) return Task.CompletedTask;
      Write($"[Mediator] HANDLER FAIL → {handlerType.Name} | Duration={duration.TotalMilliseconds:N0}ms", ConsoleColor.DarkRed);
      Write($"    Exception: {exception.Message}", ConsoleColor.DarkRed);
      return Task.CompletedTask;
    }
  }
}
