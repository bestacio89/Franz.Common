using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Business.Tests.Domain.AggregateRootTests;

public sealed class AggregateRootTests
{
  [Fact]
  public void RaiseEvent_Should_Apply_And_Track_Event()
  {
    var aggregate = new TestAggregate();

    aggregate.Create("test");

    aggregate.Name.Should().Be("test");
    aggregate.GetUncommittedChanges().Should().HaveCount(1);
    aggregate.Version.Should().Be(0);
  }

  [Fact]
  public void MarkChangesAsCommitted_Should_Clear_Changes()
  {
    var aggregate = new TestAggregate();
    aggregate.Create("test");

    aggregate.MarkChangesAsCommitted();

    aggregate.GetUncommittedChanges().Should().BeEmpty();
  }

  [Fact]
  public void ReplayEvents_Should_Rebuild_State_Without_Tracking()
  {
    var aggregate = new TestAggregate();
    var events = new[] { new TestEvent("replayed") };

    aggregate.ReplayEvents(events);

    aggregate.Name.Should().Be("replayed");
    aggregate.GetUncommittedChanges().Should().BeEmpty();
  }

  [Fact]
  public void Rehydrate_Should_Set_Id_And_Clear_Changes()
  {
    var id = Guid.NewGuid();
    var aggregate = new TestAggregate();

    aggregate.Rehydrate(id, new[] { new TestEvent("rehydrated") });

    aggregate.Id.Should().Be(id);
    aggregate.GetUncommittedChanges().Should().BeEmpty();
  }
}

