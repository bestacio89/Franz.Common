using FluentAssertions;
using Franz.Common.Logging.Extensions;
using Franz.Common.Logging.Tests.Sinks;
using Serilog;

namespace Franz.Common.Logging.Tests;

public sealed class ProductionLogFilterTests
{
  [Fact]
  public void ApplyProductionFilters_ExcludesEveryFranzCommonNamespaceEvent()
  {
    var sink = new CollectingLogEventSink();

    using var logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .ApplyProductionFilters()
        .WriteTo.Sink(sink)
        .CreateLogger();

    logger.ForContext("SourceContext", "Franz.Common")
        .Information("Framework root event");

    logger.ForContext("SourceContext", "Franz.Common.Mediator.Pipelines")
        .Error("Framework error event");

    logger.ForContext("SourceContext", "HeroService.Application.Handlers")
        .Information("Application event");

    logger.ForContext("SourceContext", "Franz.Commonish.Application")
        .Warning("Unrelated namespace event");

    sink.Events.Should().HaveCount(2);
    sink.Events
        .Select(logEvent => logEvent.Properties["SourceContext"].ToString())
        .Should()
        .BeEquivalentTo(
            "\"HeroService.Application.Handlers\"",
            "\"Franz.Commonish.Application\"");
  }

  [Fact]
  public void ApplyProductionFilters_PreservesEventsWithoutSourceContext()
  {
    var sink = new CollectingLogEventSink();

    using var logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .ApplyProductionFilters()
        .WriteTo.Sink(sink)
        .CreateLogger();

    logger.Information("Event without source context");

    sink.Events.Should().ContainSingle();
  }
}
