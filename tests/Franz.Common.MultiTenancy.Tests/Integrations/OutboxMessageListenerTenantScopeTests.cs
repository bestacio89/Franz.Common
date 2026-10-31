using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class OutboxMessageListenerTenantScopeTests
{
    [Fact]
    public async Task Listen_ExecutesOutboxMessageInsideTenantScope_AndClearsContextOnCancellation()
    {
        var stored = new StoredMessage
        {
            Body = "{}"
        };

        var messageStore = Substitute.For<IMessageStore>();
        messageStore.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StoredMessage>>([stored]));

        var inboxStore = Substitute.For<IInboxStore>();
        inboxStore.HasProcessedAsync(stored.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var dispatcher = Substitute.For<IDispatcher>();
        var serializer = Substitute.For<IMessageSerializer>();
        var initializer = new TrackingTenantMessageContextInitializer();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MessageContextAccessor>();
        services.AddSingleton<IMessageStore>(messageStore);
        services.AddSingleton<IInboxStore>(inboxStore);
        services.AddSingleton<IDispatcher>(dispatcher);
        services.AddSingleton<IMessageSerializer>(serializer);
        services.AddSingleton<ITenantMessageContextInitializer>(initializer);

        using var provider = services.BuildServiceProvider();
        var listener = new OutboxMessageListener(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxMessageListener>>());

        using var cancellation = new CancellationTokenSource();
        listener.OnMessageReceivedAsync = _ =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => listener.Listen(cancellation.Token));

        Assert.Equal(1, initializer.BeginCount);
        Assert.NotNull(initializer.LastMessage);
        Assert.Equal(1, initializer.DisposeCount);
        await messageStore.Received(1).MarkAsSentAsync(stored.Id, Arg.Any<CancellationToken>());
        await inboxStore.Received(1).MarkProcessedAsync(stored.Id, Arg.Any<CancellationToken>());
        Assert.Null(provider.GetRequiredService<MessageContextAccessor>().Current);
    }

    private sealed class TrackingTenantMessageContextInitializer : ITenantMessageContextInitializer
    {
        public int BeginCount { get; private set; }
        public int DisposeCount { get; private set; }
        public Message? LastMessage { get; private set; }

        public Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Outbox processing must pass the stored message explicitly.");

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
}
