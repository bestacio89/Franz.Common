using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.ExtensionsTests;

using Franz.Common.Business.Domain;

public class TestEnumeration : Enumeration<int>
{
  public static readonly TestEnumeration One = new(1, "One");

  public TestEnumeration(int id, string name) : base(id, name) { }
}


internal sealed class NotAnEnumeration { }

