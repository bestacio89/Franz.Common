using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class OutboxMessageScopeIsolationTests
{
    [Fact]
    public async Task Listen_ResolvesDispatcherFromANewScopeForEveryStoredMessage()
    {
        using var cancellation = new CancellationTokenSource();
        var messages = new[]
        {
            CreateStoredMessage(),
            CreateStoredMessage()
        };

        var messageStore = Substitute.For<IMessageStore>();
        var claimCount = 0;
        messageStore.ClaimPendingAsync(
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var index = Interlocked.Increment(ref claimCount) - 1;
                IReadOnlyList<StoredMessage> claimed = index < messages.Length
                    ? [messages[index]]
                    : [];
                return Task.FromResult(claimed);
            });

        var inboxStore = Substitute.For<IInboxStore>();
        inboxStore.HasProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var serializer = Substitute.For<IMessageSerializer>();
        serializer.Deserialize(Arg.Any<string>(), typeof(TestEvent)).Returns(_ => new TestEvent());

        var dispatchers = new List<IDispatcher>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageStore>(messageStore);
        services.AddSingleton<IInboxStore>(inboxStore);
        services.AddSingleton<IMessageSerializer>(serializer);
        services.AddScoped<IDispatcher>(_ =>
        {
            var dispatcher = Substitute.For<IDispatcher>();
            dispatchers.Add(dispatcher);
            dispatcher.PublishEventAsync(Arg.Any<IEvent>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            return dispatcher;
        });

        var sentCount = 0;
        messageStore.MarkAsSentAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref sentCount) == messages.Length)
                    cancellation.Cancel();
                return Task.CompletedTask;
            });

        using var provider = services.BuildServiceProvider();
        var listener = new OutboxMessageListener(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxMessageListener>>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => listener.Listen(cancellation.Token));

        Assert.Equal(messages.Length, dispatchers.Count);
        Assert.NotSame(dispatchers[0], dispatchers[1]);
        await messageStore.Received(messages.Length)
            .MarkAsSentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static StoredMessage CreateStoredMessage()
        => new()
        {
            Body = "{}",
            MessageType = typeof(TestEvent).AssemblyQualifiedName
        };

    private sealed class TestEvent : IEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    }
}
