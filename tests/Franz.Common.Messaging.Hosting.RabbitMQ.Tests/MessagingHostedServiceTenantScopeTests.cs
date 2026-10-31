#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests;

public sealed class MessagingHostedServiceTenantScopeTests
{
    [Fact]
    public async Task HandleMessage_InitializesAndDisposesTenantScope()
    {
        var listener = new StubListener();
        var initializer = new TrackingTenantMessageContextInitializer();
        var services = new ServiceCollection();
        services.AddSingleton<MessageContextAccessor>();
        services.AddSingleton<ITenantMessageContextInitializer>(initializer);
        services.AddSingleton<IMessagingStrategyExecuter, SuccessfulStrategyExecuter>();

        using var provider = services.BuildServiceProvider();
        var hostedService = new Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices.MessagingHostedService(
            listener, provider);

        await hostedService.StartAsync(CancellationToken.None);
        var message = new Message("{}");

        await listener.OnMessageReceivedAsync!(new MessageEventArgs(message));

        initializer.BeginCount.Should().Be(1);
        initializer.LastMessage.Should().BeSameAs(message);
        initializer.DisposeCount.Should().Be(1);
        provider.GetRequiredService<MessageContextAccessor>().Current.Should().BeNull();

        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HandleMessage_DisposesTenantScopeWhenHandlerFails()
    {
        var listener = new StubListener();
        var initializer = new TrackingTenantMessageContextInitializer();
        var services = new ServiceCollection();
        services.AddSingleton<MessageContextAccessor>();
        services.AddSingleton<ITenantMessageContextInitializer>(initializer);
        services.AddSingleton<IMessagingStrategyExecuter, FailingStrategyExecuter>();

        using var provider = services.BuildServiceProvider();
        var hostedService = new Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices.MessagingHostedService(
            listener, provider);

        await hostedService.StartAsync(CancellationToken.None);
        var message = new Message("{}");

        var act = () => listener.OnMessageReceivedAsync!(new MessageEventArgs(message));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated message handler failure.");

        initializer.BeginCount.Should().Be(1);
        initializer.DisposeCount.Should().Be(1);
        provider.GetRequiredService<MessageContextAccessor>().Current.Should().BeNull();

        await hostedService.StopAsync(CancellationToken.None);
    }

    private sealed class StubListener : IListener
    {
        public Func<MessageEventArgs, Task>? OnMessageReceivedAsync { get; set; }

        public Task Listen(CancellationToken stoppingToken = default) => Task.CompletedTask;

        public Task StopListenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
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

    private sealed class SuccessfulStrategyExecuter : IMessagingStrategyExecuter
    {
        public Task<bool> CanExecuteAsync(Message message) => Task.FromResult(true);
        public Task ExecuteAsync(Message message) => Task.CompletedTask;
    }

    private sealed class FailingStrategyExecuter : IMessagingStrategyExecuter
    {
        public Task<bool> CanExecuteAsync(Message message) => Task.FromResult(true);
        public Task ExecuteAsync(Message message)
            => Task.FromException(new InvalidOperationException("Simulated message handler failure."));
    }
}
