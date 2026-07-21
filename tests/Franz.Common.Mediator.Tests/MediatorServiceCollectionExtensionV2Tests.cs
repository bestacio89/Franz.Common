using FluentAssertions;
using Franz.Common.Mediator.Diagnostics;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Observers;
using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Events.Logging;
using Franz.Common.Mediator.Pipelines.Events.PostProcessing;
using Franz.Common.Mediator.Pipelines.Events.Preprocessing;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Pipelines.Processors;
using Franz.Common.Mediator.Pipelines.Processors.Logging;
using Franz.Common.Mediator.Pipelines.Processors.Validation;
using Franz.Common.Mediator.Pipelines.Resilience;
using Franz.Common.Mediator.Pipelines.Transaction;
using Franz.Common.Mediator.Pipelines.Validation;
using Franz.Common.Mediator.Validation.Events;
using Franz.Common.Mediator.Validation.Events.Preprocessing;
using Franz.Common.Mediator.Validation.Events.Validation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace Franz.Common.Mediator.Tests.Extensions;

public class MediatorServiceCollectionExtensionsV2Tests
{
  [Fact]
  public void AddFranzMediatorV2_RegistersCoreInfrastructureAndObserver()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzMediatorV2(options =>
    {
      options.EnableDefaultConsoleObserver = true;
    });

    var provider = services.BuildServiceProvider();

    // Assert
    provider.GetService<FranzMediatorOptions>().Should().NotBeNull();
    provider.GetService<IDispatcher>().Should().BeOfType<FranzDispatcher>();

    var observers = provider.GetServices<IMediatorObserver>().ToList();
    observers.Should().ContainSingle(o => o is ConsoleMediatorObserver);
  }

  [Fact]
  public void AddFranzMediatorV2Default_WiresDefaultPipelineSet()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzMediatorV2Default();

    // Assert
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(LoggingPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(ValidationPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(SerilogLoggingPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(TransactionPipeline<,>));
  }

  [Fact]
  public void AddFranzLoggingPipelineV2_RegistersAllLoggingBehaviors()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzLoggingPipelineV2();

    // Assert
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(LoggingPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(INotificationPipeline<>) && d.ImplementationType == typeof(NotificationLoggingPipeline<>));
    services.Should().Contain(d => d.ServiceType == typeof(IPreProcessor<>) && d.ImplementationType == typeof(LoggingPreProcessor<>));
    services.Should().Contain(d => d.ServiceType == typeof(IPostProcessor<,>) && d.ImplementationType == typeof(LoggingPostProcessor<,>));
  }

  [Fact]
  public void AddFranzValidationPipelineV2_RegistersValidationBehaviors()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzValidationPipelineV2();

    // Assert
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(ValidationPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(INotificationPipeline<>) && d.ImplementationType == typeof(NotificationValidationPipeline<>));
    services.Should().Contain(d => d.ServiceType == typeof(IPreProcessor<>) && d.ImplementationType == typeof(AuditPreProcessor<>));
  }

  [Fact]
  public void AddFranzTransactionPipelineV2_RegistersOptionsAndBehavior()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzTransactionPipelineV2(options =>
    {
      options.RollbackOnAnyException = false;
      options.RollbackOnExceptions.Add(typeof(InvalidOperationException));
      options.RollbackCondition = ex => ex.Message.Contains("TransactionFailed");
    });

    var provider = services.BuildServiceProvider();

    // Assert
    var options = provider.GetService<TransactionOptions>();
    options.Should().NotBeNull();
    options!.RollbackOnAnyException.Should().BeFalse();
    options.RollbackOnExceptions.Should().Contain(typeof(InvalidOperationException));
    options.RollbackCondition.Should().NotBeNull();

    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(TransactionPipeline<,>));
  }

  [Fact]
  public void AddFranzResiliencePipelinesV2_RegistersExpectedBehaviors()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzRetryPipelineV2();
    services.AddFranzTimeoutPipelineV2();
    services.AddFranzCircuitBreakerPipelineV2();
    services.AddFranzBulkheadPipelineV2();

    // Assert
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(RetryPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(TimeoutPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(CircuitBreakerPipeline<,>));
    services.Should().Contain(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(BulkheadPipeline<,>));
  }

  [Fact]
  public void AddFranzEventValidationPipelineV2_RegistersAllEventPipelineComponents()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzEventValidationPipelineV2();

    // Assert
    services.Should().Contain(d => d.ServiceType == typeof(IEventPipeline<>) && d.ImplementationType == typeof(EventValidationPipeline<>));
    services.Should().Contain(d => d.ServiceType == typeof(IEventPipeline<>) && d.ImplementationType == typeof(SerilogEventLoggingPipeline<>));

    services.Should().Contain(d => d.ServiceType == typeof(IEventPreProcessor<>) && d.ImplementationType == typeof(EventAuditPreProcessor<>));
    services.Should().Contain(d => d.ServiceType == typeof(IEventPreProcessor<>) && d.ImplementationType == typeof(SerilogEventAuditPreProcessor<>));
    services.Should().Contain(d => d.ServiceType == typeof(IEventPreProcessor<>) && d.ImplementationType == typeof(SerilogEventLoggingPreProcessor<>));

    services.Should().Contain(d => d.ServiceType == typeof(IEventPostProcessor<>) && d.ImplementationType == typeof(SerilogEventLoggingPostProcessor<>));
    services.Should().Contain(d => d.ServiceType == typeof(IEventPostProcessor<>) && d.ImplementationType == typeof(SerilogEventAuditPostProcessor<>));
    services.Should().Contain(d => d.ServiceType == typeof(IEventPostProcessor<>) && d.ImplementationType == typeof(EventAuditPostProcessor<>));
  }

  [Fact]
  public void Pipelines_MultipleInvocations_AreIdempotent()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzLoggingPipelineV2();
    services.AddFranzLoggingPipelineV2();

    // Assert - TryAddEnumerable guarantees single registration per type combination
    var loggingPipelineCount = services.Count(d => d.ServiceType == typeof(IPipeline<,>) && d.ImplementationType == typeof(LoggingPipeline<,>));
    loggingPipelineCount.Should().Be(1);
  }
}