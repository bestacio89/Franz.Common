using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Tests.Fixtures;

public sealed class KafkaTestScope : IAsyncDisposable
{
  private readonly List<Task> _backgroundTasks = new();
  private readonly List<IDisposable> _disposables = new();
  private readonly List<IAsyncDisposable> _asyncDisposables = new();

  public string BootstrapServers { get; }

  public KafkaTestScope(KafkaContainerFixture fixture)
  {
    BootstrapServers = fixture.BootstrapServers;
  }

  public T Track<T>(T obj) where T : IDisposable
  {
    _disposables.Add(obj);
    return obj;
  }

  public T TrackAsync<T>(T obj) where T : IAsyncDisposable
  {
    _asyncDisposables.Add(obj);
    return obj;
  }

  public void Track(Task task) => _backgroundTasks.Add(task);

  public async ValueTask DisposeAsync()
  {
    // 1. Wait background loops
    await Task.WhenAll(_backgroundTasks.ToArray());

    // 2. Dispose sync
    foreach (var d in _disposables)
      d.Dispose();

    // 3. Dispose async
    foreach (var d in _asyncDisposables)
      await d.DisposeAsync();
  }
}