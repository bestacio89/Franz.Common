using System.Reflection;
using FluentAssertions;
using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Persistence.EntityFramework;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Sagas.Tests.Configuration;

public sealed class SagaPersistenceScopeTests
{
  [Fact]
  public void AddFranzSagas_ResolvesRepositoryInboxOutboxAndUnitOfWorkAgainstOneScopedDbContext()
  {
    var services = new ServiceCollection();
    services.AddScoped(_ => new SagaDbContext(
        new DbContextOptionsBuilder<SagaDbContext>().Options,
        new TestDispatcher()));

    services.AddFranzSagas(options => options.Persistence = "EntityFramework");

    using var provider = services.BuildServiceProvider(validateScopes: true);
    using var scope = provider.CreateScope();

    var scopedServices = scope.ServiceProvider;
    var dbContext = scopedServices.GetRequiredService<SagaDbContext>();
    var unitOfWork = scopedServices.GetRequiredService<IUnitOfWork>();
    var inboxStore = scopedServices.GetRequiredService<IInboxStore>();
    var messageStore = scopedServices.GetRequiredService<IMessageStore>();

    scopedServices.GetRequiredService<IUnitOfWork>().Should().BeSameAs(unitOfWork);
    GetContext(unitOfWork).Should().BeSameAs(dbContext);
    GetContext(inboxStore).Should().BeSameAs(dbContext);
    GetContext(messageStore).Should().BeSameAs(dbContext);
  }

  private static SagaDbContext GetContext(object adapter)
  {
    var field = adapter.GetType().GetField(
        "_db",
        BindingFlags.Instance | BindingFlags.NonPublic);

    field.Should().NotBeNull(
        $"{adapter.GetType().Name} must retain the scoped SagaDbContext it was constructed with");

    return field!.GetValue(adapter).Should().BeOfType<SagaDbContext>().Subject;
  }

  private sealed class TestDispatcher : IDispatcher
  {
    public Task<TResponse> SendAsync<TResponse>(
        Franz.Common.Mediator.Messages.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task SendAsync(
        Franz.Common.Mediator.Messages.ICommand command,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<TResponse> SendAsync<TResponse>(
        Franz.Common.Mediator.Messages.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task PublishNotificationAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default,
        DispatchingStrategies.PublishStrategy strategy = DispatchingStrategies.PublishStrategy.Sequential,
        DispatchingStrategies.NotificationErrorHandling errorHandling =
            DispatchingStrategies.NotificationErrorHandling.ContinueOnError)
        where TNotification : Franz.Common.Mediator.Messages.INotification
        => throw new NotSupportedException();

    public Task PublishEventAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : Franz.Common.Mediator.Messages.IEvent
        => throw new NotSupportedException();
  }
}
