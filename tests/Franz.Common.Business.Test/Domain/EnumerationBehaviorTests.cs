using Franz.Common.Business.Domain;
using Xunit;
using FluentAssertions;

public sealed class OrderStatus : Enumeration<int>
{
  public static readonly OrderStatus Pending = new(1, "Pending");
  public static readonly OrderStatus Completed = new(2, "Completed");

  public OrderStatus(int id, string name) : base(id, name) { }
}

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
}
