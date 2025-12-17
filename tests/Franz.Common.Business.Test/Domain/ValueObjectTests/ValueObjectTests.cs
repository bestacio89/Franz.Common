using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Business.Tests.Domain.ValueObjectTests;

public sealed class ValueObjectTests
{
  [Fact]
  public void Equals_Should_Be_True_When_Components_Match()
  {
    var a = new Money(10, "EUR");
    var b = new Money(10, "EUR");

    a.Should().Be(b);
    (a == b).Should().BeTrue();
  }

  [Fact]
  public void Equals_Should_Be_False_When_Components_Differ()
  {
    var a = new Money(10, "EUR");
    var b = new Money(20, "EUR");

    a.Should().NotBe(b);
  }

  [Fact]
  public void GetHashCode_Should_Be_Stable_For_Equal_Objects()
  {
    var a = new Money(10, "EUR");
    var b = new Money(10, "EUR");

    a.GetHashCode().Should().Be(b.GetHashCode());
  }

  [Fact]
  public void GetCopy_Should_Return_Equal_But_Different_Instance()
  {
    var original = new Money(10, "EUR");

    var copy = original.GetCopy();

    copy.Should().Be(original);
    ReferenceEquals(copy, original).Should().BeFalse();
  }
}

