using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Dispatchers;
public readonly struct Unit
{
  private static readonly Unit _value = new Unit();
  public static Unit Value => _value;

  public override int GetHashCode() => 0;
  public override bool Equals(object? obj) => obj is Unit;
  public bool Equals(Unit other) => true;
  public override string ToString() => "()";
}
