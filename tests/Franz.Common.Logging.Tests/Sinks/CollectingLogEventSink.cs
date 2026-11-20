using Serilog.Core;
using Serilog.Events;

namespace Franz.Common.Logging.Tests.Sinks;

internal sealed class CollectingLogEventSink : ILogEventSink
{
  private readonly List<LogEvent> _events = [];

  public IReadOnlyList<LogEvent> Events => _events;

  public void Emit(LogEvent logEvent)
  {
    _events.Add(logEvent);
  }
}
