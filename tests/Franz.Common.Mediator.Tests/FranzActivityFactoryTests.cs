using System;
using System.Diagnostics;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.OpenTelemetry.Core;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Franz.Common.Mediator.Tests;

public sealed class FranzActivityFactoryTests
{
  private sealed record FindOrdersQuery;

  [Fact]
  public void StartMediatorActivity_uses_registered_source_and_ensures_correlation_id()
  {
    MediatorContext.Reset();

    try
    {
      using var source = new ActivitySource("Franz.Tests.Configured");
      using var listener = new ActivityListener
      {
        ShouldListenTo = candidate => candidate.Name == source.Name,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllDataAndRecorded
      };
      ActivitySource.AddActivityListener(listener);

      var environment = new Mock<IHostEnvironment>();
      environment.SetupGet(value => value.EnvironmentName).Returns("Testing");

      using var activity = FranzActivityFactory.StartMediatorActivity<FindOrdersQuery>(
          source,
          environment.Object);

      var startedActivity = Assert.IsType<Activity>(activity);
      Assert.Equal(source.Name, startedActivity.Source.Name);
      Assert.Equal("query", startedActivity.GetTagItem(FranzSemanticConventions.MediatorType));
      Assert.Equal("Testing", startedActivity.GetTagItem(FranzSemanticConventions.Environment));

      var correlationId = Assert.IsType<Guid>(
          startedActivity.GetTagItem(FranzSemanticConventions.CorrelationId));
      Assert.NotEqual(Guid.Empty, correlationId);
      Assert.Equal(correlationId, MediatorContext.CorrelationId);
    }
    finally
    {
      MediatorContext.Reset();
    }
  }

  [Fact]
  public void StartEventActivity_uses_registered_source_and_ensures_correlation_id()
  {
    MediatorContext.Reset();

    try
    {
      using var source = new ActivitySource("Franz.Tests.Events");
      using var listener = new ActivityListener
      {
        ShouldListenTo = candidate => candidate.Name == source.Name,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllDataAndRecorded
      };
      ActivitySource.AddActivityListener(listener);

      var environment = new Mock<IHostEnvironment>();
      environment.SetupGet(value => value.EnvironmentName).Returns("Testing");

      using var activity = FranzActivityFactory.StartEventActivity<TestEvent>(
          source,
          environment.Object);

      var startedActivity = Assert.IsType<Activity>(activity);
      Assert.Equal(source.Name, startedActivity.Source.Name);

      var correlationId = Assert.IsType<Guid>(
          startedActivity.GetTagItem(FranzSemanticConventions.CorrelationId));
      Assert.NotEqual(Guid.Empty, correlationId);
    }
    finally
    {
      MediatorContext.Reset();
    }
  }

  private sealed record TestEvent : Franz.Common.Mediator.Messages.IEvent;
}
