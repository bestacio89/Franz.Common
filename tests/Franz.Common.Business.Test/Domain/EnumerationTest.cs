using Franz.Common.Business.Domain;
using Xunit;

namespace Franz.Common.Business.Tests.Domain;

public class EnumerationTest
{
  [Fact]
  public void ToString_ReturnStringNameFromEnumeration_ResultsShippedString()
  {

    Assert.Equal("shipped", OrderStatus.Shipped.ToString());

  }
  [Fact]
  public void FromName_ReturnSubmittedEnumeration_ResultsOne()
  {

    var resultat = OrderStatus.FromName("submitted");

    Assert.Equal<int>(1, resultat.Id);

  }

  [Fact]
  public void FromValue_ReturnEnumerationFromId_ResultsEnumerationSubmitted()
  {
    var orderStatus = OrderStatus.Submitted;

    var resultat = OrderStatus.FromValue<OrderStatus>(1);

    Assert.Equal<OrderStatus>(orderStatus, resultat);

  }

  [Fact]
  public void FromDisplayName_ReturnStockConfirmedEnumeration_ResultsFive()
  {

    var resultat = OrderStatus.FromDisplayName<OrderStatus>("shipped");

    Assert.Equal<int>(5, resultat.Id);

  }

  [Fact]
  public void FromDisplayName_ReturnStockConfirmedEnumeration_ResultsThrowException()
  {

    _ = Assert.Throws<InvalidOperationException>(() => OrderStatus.FromDisplayName<OrderStatus>("notfound"));

  }

  [Fact]
  public void AbsoluteDifference_DifferenceBetweenTwoEnumeration_ResultsTwo()
  {

    var resultat = OrderStatus.AbsoluteDifference(OrderStatus.Shipped, OrderStatus.StockConfirmed);

    Assert.Equal<int>(2, resultat);

  }

  [Fact]
  public void Equals_DifferenceBetweenTwoEnumerationInstanceWithSameId_ResultsTrue()
  {
    var orderCurrentStatus = OrderStatus.Shipped;

    var resultat = OrderStatus.Equals(orderCurrentStatus, OrderStatus.Shipped);

    Assert.True(resultat);
  }

  [Fact]
  public void CompareTo_CompareTwoEnumerationOnId_ReturnLessThanZero()
  {
    var currentOrderStatus = OrderStatus.Paid;

    var resultat = currentOrderStatus.Id.CompareTo(OrderStatus.Shipped.Id);

    Assert.Equal(-1, resultat);
  }
}

public class OrderStatus : Enumeration
{
  public static OrderStatus Submitted = new(1, nameof(Submitted).ToLowerInvariant());
  public static OrderStatus AwaitingValidation = new(2, nameof(AwaitingValidation).ToLowerInvariant());
  public static OrderStatus StockConfirmed = new(3, nameof(StockConfirmed).ToLowerInvariant());
  public static OrderStatus Paid = new(4, nameof(Paid).ToLowerInvariant());
  public static OrderStatus Shipped = new(5, nameof(Shipped).ToLowerInvariant());
  public static OrderStatus Cancelled = new(6, nameof(Cancelled).ToLowerInvariant());

  public OrderStatus(int id, string name) : base(id, name)
  {
  }

  public static IEnumerable<OrderStatus> List()
  {
    return new[] { Submitted, AwaitingValidation, StockConfirmed, Paid, Shipped, Cancelled };
  }

  public static OrderStatus FromName(string name)
  {
    var state = List()
        .SingleOrDefault(s => string.Equals(s.Name, name, StringComparison.CurrentCultureIgnoreCase));

    return state ?? throw new NullReferenceException($"Possible values for OrderStatus: {string.Join(",", List().Select(s => s.Name))}");
  }

  public static OrderStatus From(int id)
  {
    var state = List().SingleOrDefault(s => s.Id == id);

    return state ?? throw new NullReferenceException($"Possible values for OrderStatus: {string.Join(",", List().Select(s => s.Name))}");
  }
}
