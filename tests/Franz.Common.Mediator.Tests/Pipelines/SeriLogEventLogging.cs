using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Events.Logging;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Mediator.Tests.Pipelines.Events;

public class SerilogEventLoggingTests
{
  public record TestDomainEvent(string Details, DateTimeOffset OccurredOn) : IEvent
  {
    public TestDomainEvent(string details) : this(details, DateTimeOffset.UtcNow) { }
  }

  private readonly Mock<ILogger<SerilogEventLoggingPipeline<TestDomainEvent>>> _pipelineLoggerMock = new();
  private readonly Mock<ILogger<SerilogEventLoggingPreProcessor<TestDomainEvent>>> _preProcessorLoggerMock = new();
  private readonly Mock<ILogger<SerilogEventLoggingPostProcessor<TestDomainEvent>>> _postProcessorLoggerMock = new();
  private readonly Mock<IHostEnvironment> _envMock = new();

  public SerilogEventLoggingTests()
  {
    _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
  }

  [Fact]
  public async Task EventLoggingLifecycle_PrePipelinePost_ExecutesWithSharedCorrelationId()
  {
    // Arrange
    MediatorContext.Reset();
    var initialContext = MediatorExecutionContext.Empty
        .WithUser("bernardo@franz.com")
        .WithTenant("tenant-fr-01");
    MediatorContext.Set(initialContext);

    var @event = new TestDomainEvent("OrderCreated");

    var preProcessor = new SerilogEventLoggingPreProcessor<TestDomainEvent>(_preProcessorLoggerMock.Object);
    var pipeline = new SerilogEventLoggingPipeline<TestDomainEvent>(_pipelineLoggerMock.Object, _envMock.Object);
    var postProcessor = new SerilogEventLoggingPostProcessor<TestDomainEvent>(_postProcessorLoggerMock.Object);

    bool handlerExecuted = false;

    // Act - 1. Pre-Process
    await preProcessor.ProcessAsync(@event, CancellationToken.None);
    var preCorrelationId = MediatorContext.CorrelationId;

    // Act - 2. Pipeline Execution
    await pipeline.HandleAsync(@event, () =>
    {
      handlerExecuted = true;
      return Task.CompletedTask;
    }, CancellationToken.None);
    var pipelineCorrelationId = MediatorContext.CorrelationId;

    // Act - 3. Post-Process
    await postProcessor.ProcessAsync(@event, CancellationToken.None);
    var postCorrelationId = MediatorContext.CorrelationId;

    // Assert
    handlerExecuted.Should().BeTrue();

    // Verify Correlation ID consistency across all phases
    preCorrelationId.Should().NotBeEmpty();
    pipelineCorrelationId.Should().Be(preCorrelationId);
    postCorrelationId.Should().Be(preCorrelationId);

    // Verify Logger Invocations
    _preProcessorLoggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting TestDomainEvent")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

    _postProcessorLoggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("handled successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  [Fact]
  public async Task EventPipeline_WhenHandlerFails_LogsErrorAndPropagatesException()
  {
    // Arrange
    MediatorContext.Reset();
    var @event = new TestDomainEvent("OrderFailed");
    var pipeline = new SerilogEventLoggingPipeline<TestDomainEvent>(_pipelineLoggerMock.Object, _envMock.Object);

    // Act
    Func<Task> act = async () => await pipeline.HandleAsync(@event, () => throw new InvalidOperationException("Event Processing Error"), CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Event Processing Error");

    _pipelineLoggerMock.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed after")),
            It.IsAny<InvalidOperationException>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }
}