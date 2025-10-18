using Ocelot.Infrastructure;
using System.Collections.Concurrent;

namespace Franz.Common.Integration.Tests.Mediator.Commands.Handlers.Events
{
  public sealed class InMemoryProcessedEventSink : IProcessedEventSink
  {
    private readonly ConcurrentBag<(string name, Guid id)> _events = new();

    public IReadOnlyCollection<(string name, Guid id)> All => _events.ToList().AsReadOnly();

    public void Add(string name, Guid aggregateId)
    {
      _events.Add((name, aggregateId));
    }

    /// <summary>
    /// Wait until a matching event appears in the sink or timeout expires.
    /// </summary>
    public async Task<(string name, Guid id)> WaitForAsync(
        string expectedName,
        TimeSpan? timeout = null,
        int pollDelayMs = 50)
    {
      var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));

      while (DateTime.UtcNow < deadline)
      {
        var match = _events.FirstOrDefault(e => e.name == expectedName);
        if (match != default && match.id != Guid.Empty)
          return match;

        await Task.Delay(pollDelayMs);
      }

      throw new TimeoutException($"Event '{expectedName}' not received within the expected time.");
    }
  }
}
