using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.ExtensionsTests;

public class DerivedEnumeration : TestEnumeration
{
  private DerivedEnumeration() : base(0, "Derived") { }
}
