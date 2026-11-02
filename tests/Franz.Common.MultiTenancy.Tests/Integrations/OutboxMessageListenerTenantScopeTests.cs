using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Contexting;
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
            Body = "{}",
            MessageType = typeof(TestEvent).AssemblyQualifiedName
        };

        var messageStore = Substitute.For<IMessageStore>();
        messageStore.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StoredMessage>>([stored]));

        var inboxStore = Substitute.For<IInboxStore>();
        inboxStore.HasProcessedAsync(stored.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var dispatcher = Substitute.For<IDispatcher>();
        var serializer = Substitute.For<IMessageSerializer>();
        serializer.Deserialize("{}", typeof(TestEvent)).Returns(new TestEvent());
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
        var messageContextAccessor = provider.GetRequiredService<MessageContextAccessor>();
        dispatcher.PublishEventAsync(Arg.Any<IEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Assert.NotNull(messageContextAccessor.Current);
                return Task.CompletedTask;
            });
        var listener = new OutboxMessageListener(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxMessageListener>>());

        using var cancellation = new CancellationTokenSource();
        listener.OnMessageReceivedAsync = _ =>
        {
            messageContextAccessor.Clear();
            return Task.CompletedTask;
        };
        messageStore.MarkAsSentAsync(stored.Id, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return Task.CompletedTask;
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => listener.Listen(cancellation.Token));

        Assert.Equal(1, initializer.BeginCount);
        Assert.NotNull(initializer.LastMessage);
        Assert.Equal(1, initializer.DisposeCount);
        await messageStore.Received(1).MarkAsSentAsync(stored.Id, Arg.Any<CancellationToken>());
        await inboxStore.Received(1).MarkProcessedAsync(stored.Id, Arg.Any<CancellationToken>());
        Assert.Null(messageContextAccessor.Current);
    }

    [Fact]
    public async Task Listen_WhenMessageCannotBeDeserialized_DoesNotMarkItProcessedOrSent()
    {
        using var cancellation = new CancellationTokenSource();
        var stored = new StoredMessage
        {
            Body = "{}",
            MessageType = typeof(TestEvent).AssemblyQualifiedName
        };

        var messageStore = Substitute.For<IMessageStore>();
        messageStore.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StoredMessage>>([stored]));

        var inboxStore = Substitute.For<IInboxStore>();
        inboxStore.HasProcessedAsync(stored.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var serializer = Substitute.For<IMessageSerializer>();
        serializer.Deserialize("{}", Arg.Any<Type>()).Returns(_ =>
        {
            cancellation.Cancel();
            return null;
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageStore>(messageStore);
        services.AddSingleton<IInboxStore>(inboxStore);
        services.AddSingleton<IDispatcher>(Substitute.For<IDispatcher>());
        services.AddSingleton<IMessageSerializer>(serializer);

        using var provider = services.BuildServiceProvider();
        var listener = new OutboxMessageListener(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxMessageListener>>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => listener.Listen(cancellation.Token));

        await inboxStore.DidNotReceive().MarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messageStore.DidNotReceive().MarkAsSentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<string, Type> ResolvableMessageTypeNames => new()
    {
        { typeof(PublicTestEvent).AssemblyQualifiedName!, typeof(PublicTestEvent) },
        { typeof(TestEvent).AssemblyQualifiedName!, typeof(TestEvent) },
        { typeof(TestEvent).FullName!, typeof(TestEvent) },
        { typeof(GenericTestEvent<string>).AssemblyQualifiedName!, typeof(GenericTestEvent<string>) }
    };

    [Theory]
    [MemberData(nameof(ResolvableMessageTypeNames))]
    public async Task Listen_ResolvesSupportedMessageTypeNames(
        string messageTypeName,
        Type expectedType)
    {
        using var cancellation = new CancellationTokenSource();
        var stored = new StoredMessage
        {
            Body = "{}",
            MessageType = messageTypeName
        };

        var messageStore = Substitute.For<IMessageStore>();
        messageStore.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StoredMessage>>([stored]));

        var inboxStore = Substitute.For<IInboxStore>();
        inboxStore.HasProcessedAsync(stored.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        Type? resolvedType = null;
        var serializer = Substitute.For<IMessageSerializer>();
        serializer.Deserialize("{}", Arg.Any<Type>()).Returns(call =>
        {
            resolvedType = call.Arg<Type>();
            cancellation.Cancel();
            return null;
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageStore>(messageStore);
        services.AddSingleton<IInboxStore>(inboxStore);
        services.AddSingleton<IDispatcher>(Substitute.For<IDispatcher>());
        services.AddSingleton<IMessageSerializer>(serializer);

        using var provider = services.BuildServiceProvider();
        var listener = new OutboxMessageListener(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxMessageListener>>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => listener.Listen(cancellation.Token));

        Assert.Equal(expectedType, resolvedType);
        await inboxStore.DidNotReceive().MarkProcessedAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messageStore.DidNotReceive().MarkAsSentAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listen_WhenMessageTypeIsUnknown_DoesNotInvokeSerializerOrMarkMessageProcessed()
    {
        using var cancellation = new CancellationTokenSource();
        var stored = new StoredMessage
        {
            Body = "{}",
            MessageType = "Unknown.Namespace.MissingMessage, Missing.Assembly"
        };

        var messageStore = Substitute.For<IMessageStore>();
        messageStore.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StoredMessage>>([stored]));

        var inboxStore = Substitute.For<IInboxStore>();
        inboxStore.HasProcessedAsync(stored.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var serializer = Substitute.For<IMessageSerializer>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageStore>(messageStore);
        services.AddSingleton<IInboxStore>(inboxStore);
        services.AddSingleton<IDispatcher>(Substitute.For<IDispatcher>());
        services.AddSingleton<IMessageSerializer>(serializer);

        using var provider = services.BuildServiceProvider();
        var listener = new OutboxMessageListener(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxMessageListener>>());

        listener.OnMessageReceivedAsync = _ =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => listener.Listen(cancellation.Token));

        serializer.DidNotReceive().Deserialize(Arg.Any<string>(), Arg.Any<Type>());
        await inboxStore.DidNotReceive().MarkProcessedAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messageStore.DidNotReceive().MarkAsSentAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private sealed class TestEvent : IEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    }

    private sealed class GenericTestEvent<T> : IEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
        public T? Value { get; init; }
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

public sealed class PublicTestEvent : IEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
