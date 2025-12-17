using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.ValueObjectTests;

internal sealed class Money : ValueObject<Money>
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
