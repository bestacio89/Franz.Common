using Franz.Common.Business.Domain;
using Xunit;
using FluentAssertions;
using Franz.Common.Business.Tests.Domain.EnumerationTests;

namespace Franz.Common.Business.Test.Domain.EnumerationTests;

public class EnumerationBehaviorTests
{
  [Fact]
  public void FromValue_ShouldReturnCorrectInstance()
  {
    var status = Enumeration<int>.FromValue<OrderStatus>(1);
    status.Should().Be(OrderStatus.Pending);
  }

  [Fact]
  public void GetAll_ShouldReturnAllInstances()
  {
    var all = Enumeration<int>.GetAll<OrderStatus>();
    all.Should().Contain(OrderStatus.Pending);
    all.Should().Contain(OrderStatus.Completed);
  }

  [Fact]
  public void GetAll_Should_Return_All_Static_Fields()
  {
    var values = Enumeration<int>.GetAll<OrderStatus>();

    values.Should().Contain(new[] { OrderStatus.Created, OrderStatus.Paid });
  }

  [Fact]
  public void FromValue_Should_Return_Correct_Enumeration()
  {
    var status = Enumeration<int>.FromValue<OrderStatus>(2);

    status.Should().Be(OrderStatus.Paid);
  }

  [Fact]
  public void FromValue_Should_Throw_For_Invalid_Value()
  {
    Action act = () => Enumeration<int>.FromValue<OrderStatus>(99);

    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void CompareTo_Should_Use_Id()
  {
    OrderStatus.Created.CompareTo(OrderStatus.Paid).Should().BeLessThan(0);
  }

  [Fact]
  public void AbsoluteDifference_Should_Return_Correct_Value()
  {
    var diff = Enumeration.AbsoluteDifference(OrderStatus.Created, OrderStatus.Paid);

    diff.Should().Be(1);
  }
}
