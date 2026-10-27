#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Messages;
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
    services.AddSingleton<IMessagingStrategyExecuter, FailingStrategyExecuter>();

    using var provider = services.BuildServiceProvider();
    using var hostedService = new KafkaMessagingHostedService(
        listener,
        provider,
        provider.GetRequiredService<ILogger<KafkaMessagingHostedService>>());

    await hostedService.StartAsync(CancellationToken.None);

    var message = new Message("{}");
    var act = () => listener.OnMessageReceivedAsync!(
        new MessageEventArgs(message));

    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Simulated downstream processing failure.");

    await hostedService.StopAsync(CancellationToken.None);
  }

  private sealed class BlockingListener : IListener
  {
    private readonly TaskCompletionSource _stopped =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

    public Task Listen(CancellationToken stoppingToken = default)
        => _stopped.Task.WaitAsync(stoppingToken);

    public Task StopListenAsync(CancellationToken cancellationToken = default)
    {
      _stopped.TrySetResult();
      return Task.CompletedTask;
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
