using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.IntegrationTesting.Domain;
public sealed class OrderLine
{
  public string Sku { get; }
  public int Quantity { get; }
  public decimal UnitPrice { get; }

  public OrderLine(string sku, int quantity, decimal unitPrice)
  {
    Sku = sku;
    Quantity = quantity;
    UnitPrice = unitPrice;
  }

  public decimal LineTotal => Quantity * UnitPrice;
}