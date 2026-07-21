using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Processors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;
using INotification = Franz.Common.Mediator.Messages.INotification;

namespace Franz.Common.Mediator.Tests.Dispatchers;

public class FranzDispatcherTests
{
  #region Test Contracts & Types

  public record TestCommand(string Input) : ICommand<string>;
  public record TestVoidCommand(string Payload) : ICommand;
  public record TestQuery(int Id) : IQuery<int>;
  public record TestNotification(string Message) : INotification;
 public record TestEvent(Guid EventId, DateTimeOffset OccurredOn) : IEvent
{
    public TestEvent(Guid eventId) : this(eventId, DateTimeOffset.UtcNow) { }
} public record TestStreamQuery(int Count) : IStreamQuery<int>;

  public class TestCommandHandler : ICommandHandler<TestCommand, string>
  {
    public Task<string> Handle(TestCommand command, CancellationToken cancellationToken)
        => Task.FromResult($"Handled:{command.Input}");
  }

  public class TestVoidCommandHandler : ICommandHandler<TestVoidCommand>
  {
    public bool Executed { get; private set; }

    public Task Handle(TestVoidCommand command, CancellationToken cancellationToken)
    {
      Executed = true;
      return Task.CompletedTask;
    }
  }

  public class TestQueryHandler : IQueryHandler<TestQuery, int>
  {
    public Task<int> Handle(TestQuery query, CancellationToken cancellationToken)
        => Task.FromResult(query.Id * 2);
  }

  public class TestNotificationHandlerOne : INotificationHandler<TestNotification>
  {
    public bool Executed { get; private set; }
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
      Executed = true;
      return Task.CompletedTask;
    }
  }

  public class TestNotificationHandlerFailing : INotificationHandler<TestNotification>
  {
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Notification Handler Failure");
  }

  public class TestEventHandler : IEventHandler<TestEvent>
  {
    public bool Executed { get; private set; }
    public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
    {
      Executed = true;
      return Task.CompletedTask;
    }
  }

  public class TestStreamQueryHandler : IStreamQueryHandler<TestStreamQuery, int>
  {
    public async IAsyncEnumerable<int> Handle(TestStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      for (int i = 1; i <= query.Count; i++)
      {
        await Task.Yield();
        yield return i;
      }
    }
  }

  public class TestPreProcessor<TRequest> : IPreProcessor<TRequest>
  {
    public bool Executed { get; private set; }
    public Task ProcessAsync(TRequest request, CancellationToken cancellationToken)
    {
      Executed = true;
      return Task.CompletedTask;
    }
  }

  public class TestPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
  {
    public bool Executed { get; private set; }
    public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
      Executed = true;
      return Task.CompletedTask;
    }
  }

  #endregion

  #region Commands & Queries Tests

  [Fact]
  public async Task SendAsync_TypedCommand_ExecutesCompiledInvokerAndReturnsResponse()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<ICommandHandler<TestCommand, string>, TestCommandHandler>();
    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act
    var result = await dispatcher.SendAsync(new TestCommand("World"));

    // Assert
    result.Should().Be("Handled:World");
  }

  [Fact]
  public async Task SendAsync_VoidCommand_ExecutesCompiledVoidInvoker()
  {
    // Arrange
    var handler = new TestVoidCommandHandler();
    var services = new ServiceCollection();
    services.AddSingleton<ICommandHandler<TestVoidCommand>>(handler);
    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act
    await dispatcher.SendAsync(new TestVoidCommand("Execute"));

    // Assert
    handler.Executed.Should().BeTrue();
  }

  [Fact]
  public async Task SendAsync_Query_ExecutesCompiledQueryInvoker()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IQueryHandler<TestQuery, int>, TestQueryHandler>();
    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act
    var result = await dispatcher.SendAsync(new TestQuery(21));

    // Assert
    result.Should().Be(42);
  }

  [Fact]
  public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var sp = new ServiceCollection().BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act & Assert
    await FluentActions.Awaiting(() => dispatcher.SendAsync((ICommand<string>)null!))
        .Should().ThrowAsync<ArgumentNullException>();
  }

  #endregion

  #region Pre and Post Processors Tests

  [Fact]
  public async Task SendAsync_InvokesPreAndPostProcessorsInPipeline()
  {
    // Arrange
    var preProcessor = new TestPreProcessor<TestCommand>();
    var postProcessor = new TestPostProcessor<TestCommand, string>();

    var services = new ServiceCollection();
    services.AddTransient<ICommandHandler<TestCommand, string>, TestCommandHandler>();
    services.AddSingleton<IPreProcessor<TestCommand>>(preProcessor);
    services.AddSingleton<IPostProcessor<TestCommand, string>>(postProcessor);

    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act
    var result = await dispatcher.SendAsync(new TestCommand("ProcessorTest"));

    // Assert
    result.Should().Be("Handled:ProcessorTest");
    preProcessor.Executed.Should().BeTrue();
    postProcessor.Executed.Should().BeTrue();
  }

  #endregion

  #region Notifications Tests

  [Fact]
  public async Task PublishNotificationAsync_SequentialStrategy_ExecutesAllHandlers()
  {
    // Arrange
    var handler = new TestNotificationHandlerOne();
    var services = new ServiceCollection();
    services.AddSingleton<INotificationHandler<TestNotification>>(handler);

    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act
    await dispatcher.PublishNotificationAsync(new TestNotification("Info"), strategy: PublishStrategy.Sequential);

    // Assert
    handler.Executed.Should().BeTrue();
  }

  [Fact]
  public async Task PublishNotificationAsync_ContinueOnError_SwallowsException()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton<INotificationHandler<TestNotification>, TestNotificationHandlerFailing>();

    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act & Assert
    var act = () => dispatcher.PublishNotificationAsync(
        new TestNotification("FailSafe"),
        strategy: PublishStrategy.Sequential,
        errorHandling: NotificationErrorHandling.ContinueOnError);

    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task PublishNotificationAsync_StopOnFirstFailure_PropagatesException()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton<INotificationHandler<TestNotification>, TestNotificationHandlerFailing>();

    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act & Assert
    var act = () => dispatcher.PublishNotificationAsync(
        new TestNotification("FailStrict"),
        strategy: PublishStrategy.Sequential,
        errorHandling: NotificationErrorHandling.StopOnFirstFailure);

    await act.Should().ThrowAsync<InvalidOperationException>();
  }

  #endregion

  #region Events Tests

  [Fact]
  public async Task PublishEventAsync_GenericAndNonGenericBridge_ExecutesHandlers()
  {
    // Arrange
    var handler = new TestEventHandler();
    var services = new ServiceCollection();
    services.AddSingleton<IEventHandler<TestEvent>>(handler);

    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);
    IEvent evt = new TestEvent(Guid.NewGuid());

    // Act
    await dispatcher.PublishEventAsync(evt);

    // Assert
    handler.Executed.Should().BeTrue();
  }

  #endregion

  #region Streaming Tests

  [Fact]
  public async Task Stream_ValidQuery_EnumeratesExpectedItems()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IStreamQueryHandler<TestStreamQuery, int>, TestStreamQueryHandler>();

    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act
    var results = new List<int>();
    await foreach (var item in dispatcher.Stream<TestStreamQuery, int>(new TestStreamQuery(3)))
    {
      results.Add(item);
    }

    // Assert
    results.Should().BeEquivalentTo(new[] { 1, 2, 3 });
  }

  #endregion

  #region Observer Notifications Tests

  [Fact]
  public async Task SendAsync_NotifiesMediatorObserverLifecycle()
  {
    // Arrange
    var observerMock = new Mock<IMediatorObserver>();
    var services = new ServiceCollection();
    services.AddTransient<ICommandHandler<TestCommand, string>, TestCommandHandler>();
    services.AddSingleton(observerMock.Object);

    var sp = services.BuildServiceProvider();
    var dispatcher = new FranzDispatcher(sp);

    // Act
    await dispatcher.SendAsync(new TestCommand("Observer"));

    // Assert
    observerMock.Verify(x => x.OnRequestStarted(
        It.IsAny<TestCommand>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

    observerMock.Verify(x => x.OnRequestCompleted(
        It.IsAny<TestCommand>(), "Handled:Observer", It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion
}