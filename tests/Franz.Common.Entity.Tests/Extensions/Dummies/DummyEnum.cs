using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Extensions.Dummies;

public class TestEnum : Enumeration<int>
{
  public static readonly TestEnum First = new(1, "First");
  private TestEnum(int id, string name) : base(id, name) { }
}