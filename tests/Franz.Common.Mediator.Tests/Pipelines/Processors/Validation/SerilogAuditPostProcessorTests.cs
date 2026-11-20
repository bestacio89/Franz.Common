using FluentAssertions;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Processors.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Franz.Common.Mediator.Tests.Pipelines.Processors.Validation;

public sealed class SerilogAuditPostProcessorTests
{
  private sealed record TestRequest;
  private sealed record TestResponse(string SensitiveValue);

  private sealed class CapturingLogger<T> : ILogger<T>
  {
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => EmptyScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
      Messages.Add(formatter(state, exception));
    }

    private sealed class EmptyScope : IDisposable
    {
      public static EmptyScope Instance { get; } = new();
      public void Dispose() { }
    }
  }

  [Fact]
  public async Task ProcessAsync_LogsMetadataWithoutDestructuringResponse_AndEstablishesCorrelation()
  {
    MediatorContext.Reset();

    var logger = new CapturingLogger<SerilogAuditPostProcessor<TestRequest, TestResponse>>();
    var environment = new Mock<IHostEnvironment>();
    environment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

    var processor = new SerilogAuditPostProcessor<TestRequest, TestResponse>(
        logger,
        environment.Object);

    await processor.ProcessAsync(new TestRequest(), new TestResponse("sensitive-response-payload"));

    logger.Messages.Should().ContainSingle();
    logger.Messages[0].Should().Contain("completed successfully");
    logger.Messages[0].Should().NotContain("sensitive-response-payload");
    MediatorContext.CorrelationId.Should().NotBeEmpty();
  }
}
