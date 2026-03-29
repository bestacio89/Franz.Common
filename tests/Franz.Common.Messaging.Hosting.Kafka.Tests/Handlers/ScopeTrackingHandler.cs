using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;
using Franz.Common.Mediator.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

public class ScopeTrackingHandler : IEventHandler<ScopeTestEvent>
{
  private readonly IServiceProvider _serviceProvider;

  // Thread-safe collection to store results from background threads
  public static readonly ConcurrentBag<int> ScopeIds = new();
  private static TaskCompletionSource<bool> _completionSource = new();
  private static int _expectedCount;

  public ScopeTrackingHandler(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  public Task HandleAsync(ScopeTestEvent @event, CancellationToken ct)
  {
    // Capture the unique ID of the current scope
    ScopeIds.Add(_serviceProvider.GetHashCode());

    if (ScopeIds.Count >= _expectedCount)
    {
      _completionSource.TrySetResult(true);
    }

    return Task.CompletedTask;
  }

  public static void Reset(int expectedCount = 3)
  {
    ScopeIds.Clear();
    _expectedCount = expectedCount;
    _completionSource = new();
  }

  public static Task WaitAsync(int count, TimeSpan timeout)
  {
    _expectedCount = count;
    return _completionSource.Task.WaitAsync(timeout);
  }
}