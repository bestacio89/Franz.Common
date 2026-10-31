using System.Text;
using Confluent.Kafka;
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.MassTransit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class MassTransitKafkaConsumerTenantScopeTests
{
    [Fact]
    public async Task SubscribeAsync_ScopedHandler_InitializesTenantContextFromKafkaHeaders()
    {
        var tenantId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var consumer = Substitute.For<IConsumer<string, string>>();
        consumer.Consume(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            return CreateDelivery(tenantId);
        });

        var initializer = new TrackingTenantMessageContextInitializer();
        var services = new ServiceCollection();
        services.AddScoped<ScopedProbe>();
        services.AddSingleton<ITenantMessageContextInitializer>(initializer);
        using var provider = services.BuildServiceProvider();

        var sut = new KafkaConsumer(consumer, provider.GetRequiredService<IServiceScopeFactory>());
        var handlerExecuted = false;

        await sut.SubscribeAsync<Payload>(
            "tenant-events",
            (scopedServices, payload) =>
            {
                Assert.Equal("verified", payload.Value);
                Assert.NotNull(scopedServices.GetRequiredService<ScopedProbe>());
                handlerExecuted = true;
                return Task.CompletedTask;
            },
            cancellation.Token);

        Assert.True(handlerExecuted);
        Assert.Equal(1, initializer.BeginCount);
        Assert.Equal(1, initializer.DisposeCount);
        Assert.Equal(tenantId.ToString(), initializer.LastMessage!.Headers[HeaderConstants.TenantId].Single());
    }

    [Fact]
    public async Task SubscribeAsync_LegacyHandler_FailsClosedWhenTenantProcessingIsEnabled()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        consumer.Consume(Arg.Any<CancellationToken>()).Returns(CreateDelivery(Guid.NewGuid()));

        var services = new ServiceCollection();
        services.AddSingleton<ITenantMessageContextInitializer>(new TrackingTenantMessageContextInitializer());
        using var provider = services.BuildServiceProvider();

        var sut = new KafkaConsumer(consumer, provider.GetRequiredService<IServiceScopeFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SubscribeAsync<Payload>("tenant-events", _ => Task.CompletedTask));
    }

    private static ConsumeResult<string, string> CreateDelivery(Guid tenantId)
    {
        var headers = new Headers
        {
            new Header(HeaderConstants.TenantId, Encoding.UTF8.GetBytes(tenantId.ToString()))
        };

        return new ConsumeResult<string, string>
        {
            Topic = "tenant-events",
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Confluent.Kafka.Message<string, string>
            {
                Value = "{\"Value\":\"verified\"}",
                Headers = headers
            }
        };
    }

    private sealed class Payload
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class ScopedProbe;

    private sealed class TrackingTenantMessageContextInitializer : ITenantMessageContextInitializer
    {
        public int BeginCount { get; private set; }
        public int DisposeCount { get; private set; }
        public Message? LastMessage { get; private set; }

        public Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The consumer must pass the Kafka delivery explicitly.");

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
