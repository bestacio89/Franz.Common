using Franz.Common.Business.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Franz.Common.EntityFramework;

public class EnumerationConverter<TEnumeration, TId> : ValueConverter<TEnumeration, TId>
  where TEnumeration : Enumeration<TId>
  where TId : notnull, IComparable<TId>
{
  public EnumerationConverter()
      : base(
          v => v.Id,
          v => Enumeration<TId>.FromValue<TEnumeration>(v)
      )
  {
  }
}
