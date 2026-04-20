using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Extensions.Dummies;

public class TestEnum : Enumeration<int>
{
  public static readonly TestEnum One = new(1, "One");
  public static readonly TestEnum First = new(2, "First");
  private TestEnum(int id, string name) : base(id, name) { }
}