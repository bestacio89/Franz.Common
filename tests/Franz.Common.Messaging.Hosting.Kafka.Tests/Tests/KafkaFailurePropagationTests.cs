using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy;
using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.Headers;
using System.Collections.Concurrent;
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

  [Fact]
  public async Task ConcurrentDeliveries_KeepTenantExecutionContextsIsolated()
  {
    var tenantA = new TenantInfo(Guid.NewGuid(), "tenant-a");
    var tenantB = new TenantInfo(Guid.NewGuid(), "tenant-b");
    var listener = new BlockingListener();
    var observedTenants = new ConcurrentDictionary<Guid, Guid>();
    var accessors = new ConcurrentBag<ITenantExecutionContextAccessor>();
    var bothHandlersStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseHandlers = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddScoped<MessageContextAccessor>();
    services.AddScoped<IMessageContextAccessor>(sp => sp.GetRequiredService<MessageContextAccessor>());
    services.AddScoped<ITenantExecutionContextAccessor, MessagingTenantExecutionContextAccessor>();
    services.AddScoped<ITenantMessageContextInitializer, TenantMessageContextInitializer>();
    services.AddScoped<ITenantMessageAuthorizer, AllowAllTenantMessages>();
    services.AddSingleton<ITenantStore>(new TestTenantStore(tenantA, tenantB));
    services.AddScoped<IMessagingStrategyExecuter>(sp =>
    {
      var accessor = sp.GetRequiredService<ITenantExecutionContextAccessor>();
      accessors.Add(accessor);
      return new ScopedTenantProbeExecuter(accessor, observedTenants, bothHandlersStarted, releaseHandlers);
    });

    using var provider = services.BuildServiceProvider();
    using var hostedService = new KafkaMessagingHostedService(
        listener,
        provider,
        provider.GetRequiredService<ILogger<KafkaMessagingHostedService>>());

    await hostedService.StartAsync(CancellationToken.None);
    await listener.HandlerReady.WaitAsync(TimeSpan.FromSeconds(5));

    var messageA = CreateTenantMessage(tenantA.Id);
    var messageB = CreateTenantMessage(tenantB.Id);

    var deliveryA = listener.OnMessageReceivedAsync!(new MessageEventArgs(messageA));
    var deliveryB = listener.OnMessageReceivedAsync!(new MessageEventArgs(messageB));

    await bothHandlersStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    releaseHandlers.TrySetResult();
    await Task.WhenAll(deliveryA, deliveryB);

    observedTenants[messageA.Id].Should().Be(tenantA.Id);
    observedTenants[messageB.Id].Should().Be(tenantB.Id);
    accessors.Should().HaveCount(2);
    Assert.All(accessors, accessor => Assert.Null(accessor.Current));
    await hostedService.StopAsync(CancellationToken.None);
  }

  private static Message CreateTenantMessage(Guid tenantId)
  {
    var message = new Message("{}");
    message.Headers[HeaderConstants.TenantId] = [tenantId.ToString()];
    return message;
  }

  private sealed class TestTenantStore(params TenantInfo[] tenants) : ITenantStore
  {
    private readonly IReadOnlyDictionary<Guid, TenantInfo> _tenants =
        tenants.ToDictionary(tenant => tenant.Id);

    public Task<TenantInfo?> FindByIdAsync(Guid id)
        => Task.FromResult(_tenants.GetValueOrDefault(id));

    public Task<TenantInfo?> FindByHostAsync(string host)
        => Task.FromResult<TenantInfo?>(null);

    public Task<IEnumerable<TenantInfo>> GetAllAsync()
        => Task.FromResult<IEnumerable<TenantInfo>>(_tenants.Values);

    public Task<bool> ExistsAsync(Guid id)
        => Task.FromResult(_tenants.ContainsKey(id));
  }

  private sealed class AllowAllTenantMessages : ITenantMessageAuthorizer
  {
    public Task<bool> IsAuthorizedAsync(
        TenantInfo tenant,
        Message message,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);
  }

  private sealed class ScopedTenantProbeExecuter(
      ITenantExecutionContextAccessor accessor,
      ConcurrentDictionary<Guid, Guid> observedTenants,
      TaskCompletionSource bothHandlersStarted,
      TaskCompletionSource releaseHandlers) : IMessagingStrategyExecuter
  {
    public Task<bool> CanExecuteAsync(Message message) => Task.FromResult(true);

    public async Task ExecuteAsync(Message message)
    {
      var tenantId = accessor.Current?.TenantId
          ?? throw new InvalidOperationException("Tenant context was not established for the message.");
      observedTenants[message.Id] = tenantId;

      if (observedTenants.Count == 2)
        bothHandlersStarted.TrySetResult();

      await releaseHandlers.Task.ConfigureAwait(false);
    }
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
