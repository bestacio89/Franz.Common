using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.EnumerationTests;

internal sealed class OrderStatus : Enumeration<int>
{
  public static readonly OrderStatus Created = new(1, "Created");
  public static readonly OrderStatus Paid = new(2, "Paid");
  public static readonly OrderStatus Pending = new(1, "Pending");
  public static readonly OrderStatus Completed = new(2, "Completed");

  private OrderStatus(int id, string name) : base(id, name) { }
}

