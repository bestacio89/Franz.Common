using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class MediatorContextPropagationTests
{
  [Fact]
  public void BeginScope_RestoresPreviousContextAfterDisposal()
  {
    var original = new MediatorExecutionContext
    {
      CorrelationId = Guid.CreateVersion7(),
      CausationId = Guid.CreateVersion7(),
      TenantId = "tenant-original"
    };

    MediatorContext.Set(original);

    try
    {
      var scoped = new MediatorExecutionContext
      {
        CorrelationId = Guid.CreateVersion7(),
        CausationId = Guid.CreateVersion7(),
        TenantId = "tenant-scoped"
      };

      using (MediatorContext.BeginScope(scoped))
      {
        MediatorContext.Current.Should().Be(scoped);
      }

      MediatorContext.Current.Should().Be(original);
    }
    finally
    {
      MediatorContext.Reset();
    }
  }

  [Fact]
  public void IntegrationEventMessageBuilder_PropagatesCorrelationCausationAndTenant()
  {
    var correlationId = Guid.CreateVersion7();
    var causationId = Guid.CreateVersion7();
    var context = new MediatorExecutionContext
    {
      CorrelationId = correlationId,
      CausationId = causationId,
      TenantId = "tenant-42"
    };

    using (MediatorContext.BeginScope(context))
    {
      var message = new IntegrationEventMessageBuilderStrategy().Build(new TestIntegrationEvent());

      message.CorrelationId.Should().Be(correlationId);
      message.Headers[Message.CausationHeader].Should().ContainSingle()
          .Which.Should().Be(causationId.ToString());
      message.Headers["TenantId"].Should().ContainSingle()
          .Which.Should().Be("tenant-42");
    }
  }

  private sealed record TestIntegrationEvent : IIntegrationEvent;
}
