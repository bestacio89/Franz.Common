using Franz.Common.Business.Domain;
using Xunit;
using FluentAssertions;

public sealed class Money : ValueObject<Money>
{
  public decimal Amount { get; }
  public string Currency { get; }

  public Money(decimal amount, string currency)
  {
    Amount = amount;
    Currency = currency;
  }

  protected override IEnumerable<object?> GetEqualityComponents()
  {
    yield return Amount;
    yield return Currency;
  }
}

public class ValueObjectEqualityTests
{
  [Fact]
  public void ValueObjects_WithSameComponents_AreEqual()
  {
    var m1 = new Money(100, "EUR");
    var m2 = new Money(100, "EUR");

    m1.Should().Be(m2);
  }
}
