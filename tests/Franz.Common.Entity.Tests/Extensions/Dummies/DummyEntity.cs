using System;
using System.Collections.Generic;
using System.Text;
using Franz.Common.Business.Domain;

namespace Franz.Common.EntityFramework.Tests.Extensions.Dummies;

public class DummyEntity : Entity
{
  public int Id { get; set; }
  // Ensure this property is public and has a getter/setter
  public TestEnum EnumProp { get; set; } = TestEnum.First;
}

