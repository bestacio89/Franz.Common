#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Tests;

public sealed class KafkaFailurePropagationTests
{
  [Fact]
  public async Task HandlerFailure_IsPropagatedToTransportListener()
  {
    var listener = new BlockingListener();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<MessageContextAccessor>();
    var tenantInitializer = new TrackingTenantMessageContextInitializer();
    services.AddSingleton<ITenantMessageContextInitializer>(tenantInitializer);
    services.AddSingleton<IMessagingStrategyExecuter, FailingStrategyExecuter>();

    using var provider = services.BuildServiceProvider();
    using var hostedService = new KafkaMessagingHostedService(
        listener,
        provider,
        provider.GetRequiredService<ILogger<KafkaMessagingHostedService>>());

    await hostedService.StartAsync(CancellationToken.None);
    await listener.HandlerReady.WaitAsync(TimeSpan.FromSeconds(5));

    var message = new Message("{}");
    var act = () => listener.OnMessageReceivedAsync!(
        new MessageEventArgs(message));

    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Simulated downstream processing failure.");

    tenantInitializer.BeginCount.Should().Be(1);
    tenantInitializer.LastMessage.Should().BeSameAs(message);
    tenantInitializer.DisposeCount.Should().Be(1,
        "tenant execution context must be cleared even when message handling fails");
    provider.GetRequiredService<MessageContextAccessor>().Current.Should().BeNull(
        "ambient message context must be cleared after a failed delivery");

    await hostedService.StopAsync(CancellationToken.None);
  }

  private sealed class BlockingListener : IListener
  {
    private readonly TaskCompletionSource _stopped =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _handlerReady =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }
    public Task HandlerReady => _handlerReady.Task;

    public Task Listen(CancellationToken stoppingToken = default)
    {
      _handlerReady.TrySetResult();
      return _stopped.Task.WaitAsync(stoppingToken);
    }

    public Task StopListenAsync(CancellationToken cancellationToken = default)
    {
      _stopped.TrySetResult();
      return Task.CompletedTask;
    }
  }

 
  private sealed class TrackingTenantMessageContextInitializer : ITenantMessageContextInitializer
  {
    public int BeginCount { get; private set; }
    public int DisposeCount { get; private set; }
    public Message? LastMessage { get; private set; }

    public Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The hosted consumer must pass the delivered message explicitly.");

    public Task<IAsyncDisposable> BeginAsync(Message message, CancellationToken cancellationToken = default)
    {
      LastMessage = message;
      BeginCount++;
      return Task.FromResult<IAsyncDisposable>(new ExecutionScope(this));
    }

    private sealed class ExecutionScope(TrackingTenantMessageContextInitializer owner) : IAsyncDisposable
    {
      public ValueTask DisposeAsync()
      {
        owner.DisposeCount++;
        return ValueTask.CompletedTask;
      }
    }
  }

  private sealed class FailingStrategyExecuter : IMessagingStrategyExecuter
  {
    public Task<bool> CanExecuteAsync(Message message) => Task.FromResult(true);

    public Task ExecuteAsync(Message message)
        => Task.FromException(new InvalidOperationException(
            "Simulated downstream processing failure."));
  }
}
