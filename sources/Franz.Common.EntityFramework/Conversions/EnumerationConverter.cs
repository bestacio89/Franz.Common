using System.Linq.Expressions;
using Franz.Common.Business.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Franz.Common.EntityFramework;

public class EnumerationConverter<TEnumeration, TId> : ValueConverter<TEnumeration, TId>
  where TEnumeration : Enumeration<TId>
  where TId : notnull, IComparable<TId>
{
  public EnumerationConverter()
      : base(
          v => v == null ? ThrowToProvider() : v.Id,
          v => v == null ? ThrowFromProvider() : Enumeration<TId>.FromValue<TEnumeration>(v)
      )
  {
  }

  // Matches the Return Type of the 'ToProvider' Expression (TId)
  private static TId ThrowToProvider() =>
      throw new ArgumentNullException("v", "Enumeration cannot be null");

  // Matches the Return Type of the 'FromProvider' Expression (TEnumeration)
  private static TEnumeration ThrowFromProvider() =>
      throw new ArgumentNullException("v", "Id value cannot be null");
}